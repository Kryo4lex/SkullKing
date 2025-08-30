using SkullKingCore.Utility;

namespace SkullKingSandboxConsole
{
    public static class Settings
    {

        static Settings()
        {
            RandomProvider.SetSeed(0);
        }

        public const int DecimalPlaces = 6;

        public const int MinPlayerCount = 2;

        public const int MaxPlayerCount = 8;

        public const int MaxRounds = 10;

    }
}
