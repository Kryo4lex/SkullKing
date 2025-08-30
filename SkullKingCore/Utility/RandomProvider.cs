using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace SkullKingCore.Utility
{

    /// <summary>
    /// Random utility for .NET 8:
    /// - Unseeded: uses Random.Shared (fast, thread-safe, non-deterministic).
    /// - Seeded: reproducible sequences via a single locked RNG.
    /// - Seeded parallel: use GetStream(streamId) for deterministic per-stream RNGs.
    /// </summary>
    public static class RandomProvider
    {
        // --- State ---
        private static readonly object _lock = new object();
        private static volatile bool _isSeeded;
        private static volatile int _baseSeed;

        // Single seeded RNG for fully reproducible global calls
        private static Random _seededSingleton = new Random(123456789); // replaced on SetSeed

        // Optional deterministic per-stream RNGs for parallel code
        private static ConcurrentDictionary<int, Random>? _streams;

        // ---------- Public API ----------

        /// <summary>Enable deterministic mode with the given seed.</summary>
        public static void SetSeed(int seed)
        {
            _baseSeed = seed;
            _isSeeded = true;

            lock (_lock)
            {
                _seededSingleton = new Random(seed);
                // Reset deterministic streams
                _streams = new ConcurrentDictionary<int, Random>();
            }
        }

        /// <summary>Disable deterministic mode; reverts to Random.Shared (non-deterministic, high performance).</summary>
        public static void DisableSeed()
        {
            _isSeeded = false;
            lock (_lock)
            {
                _streams = null;
                // Reinitialize singleton with secure entropy to avoid accidental reuse
                _seededSingleton = new Random(CreateSecureSeed());
            }
        }

        /// <summary>Is deterministic seeding currently enabled?</summary>
        public static bool IsSeeded => _isSeeded;

        // Convenience forwarders (use these if you don't need parallel deterministic streams)
        public static int Next()
        {
            if (_isSeeded)
            {
                lock (_lock) return _seededSingleton.Next();
            }
            return Random.Shared.Next();
        }

        public static int Next(int maxValue)
        {
            if (_isSeeded)
            {
                lock (_lock) return _seededSingleton.Next(maxValue);
            }
            return Random.Shared.Next(maxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            if (_isSeeded)
            {
                lock (_lock) return _seededSingleton.Next(minValue, maxValue);
            }
            return Random.Shared.Next(minValue, maxValue);
        }

        public static double NextDouble()
        {
            if (_isSeeded)
            {
                lock (_lock) return _seededSingleton.NextDouble();
            }
            return Random.Shared.NextDouble();
        }

        public static void NextBytes(byte[] buffer)
        {
            if (_isSeeded)
            {
                lock (_lock) _seededSingleton.NextBytes(buffer);
                return;
            }
            Random.Shared.NextBytes(buffer);
        }

        /// <summary>
        /// Get a deterministic per-stream Random. Use a stable streamId (e.g., partition index 0..N-1).
        /// Same (seed, streamId) => same sequence across runs, independent of thread scheduling.
        /// </summary>
        public static Random GetStream(int streamId)
        {
            if (!_isSeeded)
            {
                // In unseeded mode, no determinism guarantee—just return a fast RNG.
                return Random.Shared;
            }

            var dict = _streams ?? throw new InvalidOperationException("Seeded mode not initialized.");
            return dict.GetOrAdd(streamId, id => new Random(DeriveSeed(_baseSeed, id)));
        }

        // ---------- internals ----------

        /// <summary>Cryptographically strong 32-bit seed for unseeded reinitializations.</summary>
        private static int CreateSecureSeed()
        {
            Span<byte> b = stackalloc byte[4];
            RandomNumberGenerator.Fill(b);
            return BitConverter.ToInt32(b);
        }

        /// <summary>
        /// Derive a well-distributed 32-bit seed from (baseSeed, streamId).
        /// SplitMix64-style mixer for good diffusion and repeatability.
        /// </summary>
        private static int DeriveSeed(int baseSeed, int streamId)
        {
            ulong z = ((ulong)(uint)baseSeed << 1) ^ (ulong)(uint)streamId;
            z += 0x9E3779B97F4A7C15ul;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9ul;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBul;
            z ^= (z >> 31);
            return unchecked((int)z);
        }
    }

}
