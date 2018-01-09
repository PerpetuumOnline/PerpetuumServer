namespace Perpetuum.Services.Looting
{
    public static class LootHelper
    {
        private const double DROP_CHANCE = 0.5;

        public static bool Roll(double chance = DROP_CHANCE)
        {
            return FastRandom.NextDouble() <= chance;               
        }

        public static string PinToString(int pinCode)
        {
            return pinCode == 0 ? "(empty)" : $"{(char) (pinCode & 255)}{(char) ((pinCode >> 8) & 255)}{(char) ((pinCode >> 16) & 255)}{(char) ((pinCode >> 24) & 255)}";
        }
    }
}