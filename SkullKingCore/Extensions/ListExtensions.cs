namespace SkullKingCore.Extensions
{
    public static class ListExtensions
    {
        private static Random staticRng = new Random(1);

        public static List<T> Shuffle<T>(this List<T> list, int seed)
        {
            int n = list.Count;

            Random rng = new Random(seed);

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1); // Random index from 0 to n

                // Swap list[k] and list[n] using a temporary variable
                T temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }

            return list;
        }

        public static List<T> Shuffle<T>(this List<T> list, Random rng)
        {
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1); // Random index from 0 to n

                // Swap list[k] and list[n] using a temporary variable
                T temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }

            return list;
        }

        public static List<T> Shuffle<T>(this List<T> list)
        {
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = staticRng.Next(n + 1); // Random index from 0 to n

                // Swap list[k] and list[n] using a temporary variable
                T temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }

            return list;
        }

        public static List<T> TakeChunk<T>(this List<T> source, int count)
        {
            int actualCount = Math.Min(count, source.Count);
            List<T> chunk = source.GetRange(0, actualCount);
            source.RemoveRange(0, actualCount);
            return chunk;
        }
    }
}
