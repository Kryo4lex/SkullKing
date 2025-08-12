namespace SkullKingCore.Utility
{
    public static class RandomHelper
    {

        private static readonly Random rng = new Random();

        public static int RandomInt(int min, int max)
        {
            return rng.Next(min, max);
        }

    }
}
