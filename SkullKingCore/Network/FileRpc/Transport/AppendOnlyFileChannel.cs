using System.Text;

namespace SkullKingCore.Network.FileRpc.Transport
{
    /// <summary>
    /// Event-driven mailbox file:
    /// - Writers append a line and close quickly.
    /// - Reader wakes on FS events (Changed/Created/Renamed) or a rare fallback tick.
    /// - After reading anything, TRUNCATE file to 0 bytes (prevents bloat).
    /// 
    /// Designed for minimal CPU: no busy polling, tiny I/O.
    /// </summary>
    internal sealed class AppendOnlyFileChannel : IAsyncDisposable
    {
        private readonly string _path;
        private readonly Encoding _enc = new UTF8Encoding(false);
        private readonly FileSystemWatcher _watcher;
        private readonly SemaphoreSlim _signal = new(0, int.MaxValue);
        private readonly TimeSpan _fallbackWake = TimeSpan.FromSeconds(5); // safety net

        public string Path => _path;

        public AppendOnlyFileChannel(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            _path = path;

            var dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Ensure mailbox exists
            using var _ = new FileStream(_path, FileMode.OpenOrCreate, FileAccess.Read,
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
            try { _signal.Release(); } catch { /* ignore if already signaled */ }
        }

        /// <summary>Append one line (envelope) and flush/close fast.</summary>
        public async Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            for (; ; )
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var fs = new FileStream(_path, FileMode.Append, FileAccess.Write,
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
        /// Wake on FS event (or rare timeout), drain lines, truncate if anything was read,
        /// then yield the drained lines (outside of try/catch to satisfy C# iterator rules).
        /// </summary>
        public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for a signal or periodic fallback
                bool gotSignal = false;
                try
                {
                    gotSignal = await _signal.WaitAsync(_fallbackWake, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                List<string>? batch = null;

                // Drain inside try, but DO NOT yield here
                try
                {
                    batch = await DrainOnceAsync(ct).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // transient I/O — just try again
                }

                if (batch is { Count: > 0 })
                {
                    // Now it's safe to yield (outside try/catch)
                    foreach (var line in batch)
                        yield return line;

                    // Immediately look again to catch bursts with minimal latency
                    TryRelease();
                }
                else if (!gotSignal)
                {
                    // Timeout but nothing to read — just loop again
                }
            }
        }

        /// <summary>Read all lines currently in the file and truncate to 0 if any were read.</summary>
        private async Task<List<string>> DrainOnceAsync(CancellationToken ct)
        {
            var list = new List<string>(16);

            using (var fs = new FileStream(_path, FileMode.OpenOrCreate, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 8192,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var sr = new StreamReader(fs, _enc, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: false))
            {
                string? line;
                while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    line = line.TrimEnd('\r');
                    if (line.Length == 0) continue;
                    list.Add(line);
                }
            }

            if (list.Count > 0)
            {
                // Truncate (not delete) to avoid handle races and keep inode stable.
                try
                {
                    using var trunc = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite,
                        FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.Asynchronous);
                    trunc.SetLength(0);
                }
                catch (IOException)
                {
                    // transient; ignore
                }
            }

            return list;
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
