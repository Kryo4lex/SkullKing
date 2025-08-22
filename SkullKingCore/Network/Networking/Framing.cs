#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SkullKingCore.Network.Networking
{
    /// <summary>
    /// Length-prefixed framing with a 4-byte big-endian header.
    /// No Span/stackalloc so it compiles on C# 12.
    /// </summary>
    public static class Framing
    {
        public const int MaxFrameBytes = 16 * 1024 * 1024; // 16 MB

        public static async Task WriteFrameAsync(Stream stream, byte[]? payload, CancellationToken ct)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            payload ??= Array.Empty<byte>();

            if (payload.Length > MaxFrameBytes)
                throw new InvalidOperationException($"Frame too large: {payload.Length} (max {MaxFrameBytes}).");

            // Build 4-byte big-endian header without Span/stackalloc
            var header = new byte[4];
            int len = payload.Length;
            header[0] = (byte)((len >> 24) & 0xFF);
            header[1] = (byte)((len >> 16) & 0xFF);
            header[2] = (byte)((len >> 8) & 0xFF);
            header[3] = (byte)(len & 0xFF);

            await stream.WriteAsync(header, 0, 4, ct).ConfigureAwait(false);
            if (len > 0)
                await stream.WriteAsync(payload, 0, len, ct).ConfigureAwait(false);

            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        public static async Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken ct)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            // Read 4-byte header
            var header = await ReadExactAsync(stream, 4, ct).ConfigureAwait(false);
            if (header.Length == 0) return null; // remote closed

            int len = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
            if (len < 0 || len > MaxFrameBytes)
                throw new InvalidOperationException($"Invalid frame length: {len}.");

            if (len == 0) return Array.Empty<byte>();
            return await ReadExactAsync(stream, len, ct).ConfigureAwait(false);
        }

        private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken ct)
        {
            var buffer = new byte[count];
            int read = 0;

            while (read < count)
            {
                int n = await stream.ReadAsync(buffer, read, count - read, ct).ConfigureAwait(false);
                if (n == 0)
                {
                    // EOF
                    if (read == 0) return Array.Empty<byte>();
                    throw new EndOfStreamException($"Unexpected end of stream (needed {count}, got {read}).");
                }
                read += n;
            }

            return buffer;
        }
    }
}
