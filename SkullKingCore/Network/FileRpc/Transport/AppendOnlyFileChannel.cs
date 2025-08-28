using System.Text;

namespace SkullKingCore.Network.FileRpc.Transport
{
    /// <summary>
    /// Deadlock-safe, event-driven append-only mailbox without Span usage (C# 12 compatible):
    /// - Writers append a line and close quickly.
    /// - Reader wakes on FS events or a short fallback tick.
    /// - Reader keeps a byte offset and only consumes COMPLETE lines (ending with '\n').
    /// - Reader truncates to zero ONLY when it has consumed exactly to EOF (after a length re-check).
    /// </summary>
    internal sealed class AppendOnlyFileChannel : IAsyncDisposable
    {
        private readonly string _path;
        private readonly Encoding _enc = new UTF8Encoding(false);
        private readonly FileSystemWatcher _watcher;
        private readonly SemaphoreSlim _signal = new(0, int.MaxValue);
        private readonly TimeSpan _fallbackWake = TimeSpan.FromSeconds(1);
        private long _readOffset; // bytes fully consumed

        public string Path => _path;

        public AppendOnlyFileChannel(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            _path = path;

            var dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Ensure mailbox exists
            using var _ = new FileStream(
                _path, FileMode.OpenOrCreate, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.Asynchronous);

            // Event-driven watcher
            var watchDir = string.IsNullOrEmpty(dir) ? "." : dir!;
            var fileName = System.IO.Path.GetFileName(_path);
            _watcher = new FileSystemWatcher(watchDir, fileName)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            _watcher.Changed += (_, __) => TryRelease();
            _watcher.Created += (_, __) => TryRelease();
            _watcher.Renamed += (_, __) => TryRelease();
            _watcher.EnableRaisingEvents = true;

            // kick once in case file already has content
            TryRelease();
        }

        private void TryRelease()
        {
            try { _signal.Release(); } catch { /* ignore */ }
        }

        /// <summary>Append one line (envelope) and flush/close fast.</summary>
        public async Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            for (; ; )
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var fs = new FileStream(
                        _path, FileMode.Append, FileAccess.Write,
                        FileShare.ReadWrite | FileShare.Delete, 4096,
                        FileOptions.Asynchronous | FileOptions.WriteThrough | FileOptions.SequentialScan);
                    using var sw = new StreamWriter(fs, _enc);
                    await sw.WriteLineAsync(line.AsMemory(), ct).ConfigureAwait(false);
                    await sw.FlushAsync().ConfigureAwait(false);
                    return;
                }
                catch (IOException)
                {
                    await Task.Delay(3, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Event-driven reading:
        /// - Wait for a signal (or short fallback).
        /// - Read from _readOffset to current file length.
        /// - Process only up to the last '\n' byte.
        /// - Advance _readOffset to processed end.
        /// - Truncate to 0 only if we consumed to the file's end (and length hasn't changed).
        /// </summary>
        public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(_fallbackWake, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                List<string>? batch = null;

                try
                {
                    batch = await DrainOnceAsync(ct).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // transient; try again on next event/tick
                }

                if (batch is { Count: > 0 })
                {
                    foreach (var line in batch)
                        yield return line;

                    // Immediately look again to capture bursts with minimal latency
                    TryRelease();
                }
            }
        }

        /// <summary>
        /// Drain from current offset, only up to the last newline. No Span usage (C# 12 safe).
        /// </summary>
        private async Task<List<string>> DrainOnceAsync(CancellationToken ct)
        {
            var lines = new List<string>(16);

            long lengthAtOpen;
            byte[]? buffer = null;
            int bytesReadTotal = 0;

            // 1) Snapshot length and read bytes from offset
            using (var fs = new FileStream(
                _path, FileMode.OpenOrCreate, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 8192,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                if (_readOffset > fs.Length)
                    _readOffset = 0;

                lengthAtOpen = fs.Length;
                if (lengthAtOpen <= _readOffset)
                    return lines;

                var toRead = checked((int)(lengthAtOpen - _readOffset));
                buffer = new byte[toRead];
                fs.Seek(_readOffset, SeekOrigin.Begin);
                int n;
                while (bytesReadTotal < toRead &&
                       (n = await fs.ReadAsync(buffer, bytesReadTotal, toRead - bytesReadTotal, ct).ConfigureAwait(false)) > 0)
                {
                    bytesReadTotal += n;
                }
            }

            if (bytesReadTotal == 0 || buffer is null)
                return lines;

            // 2) Find last newline within the freshly read bytes
            int lastNewlineIdx = -1;
            for (int i = bytesReadTotal - 1; i >= 0; i--)
            {
                if (buffer[i] == (byte)'\n') { lastNewlineIdx = i; break; }
            }

            if (lastNewlineIdx < 0)
            {
                // No complete lines yet
                return lines;
            }

            int processLen = lastNewlineIdx + 1; // include '\n'

            // 3) Decode only the complete portion and split into lines (no Span)
            var text = _enc.GetString(buffer, 0, processLen);

            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    int len = i - start;
                    if (len > 0 && text[i - 1] == '\r') len--;
                    if (len > 0)
                    {
                        var s = text.Substring(start, len);
                        lines.Add(s);
                    }
                    start = i + 1;
                }
            }

            // 4) Advance offset by consumed bytes
            var newOffset = _readOffset + processLen;
            _readOffset = newOffset;

            // 5) Safe truncate if we consumed exactly to EOF (with re-check)
            if (newOffset == lengthAtOpen)
            {
                try
                {
                    using var trunc = new FileStream(
                        _path, FileMode.Open, FileAccess.ReadWrite,
                        FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.Asynchronous);

                    if (trunc.Length == lengthAtOpen)
                    {
                        trunc.SetLength(0);
                        _readOffset = 0;
                    }
                }
                catch (IOException)
                {
                    // if a writer raced us, skip truncation
                }
            }

            return lines;
        }

        public ValueTask DisposeAsync()
        {
            try { _watcher.EnableRaisingEvents = false; } catch { }
            try { _watcher.Dispose(); } catch { }
            _signal.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
