namespace AntiCheat
{
    public class Anticheat
    {
        public static void TriggerAnticheat()
        {
            GameDataManager.powerPrefs.dontUploadToLeaderboard = true;
            AnticheatInternal.AnticheatTriggered = true;
        }

        public static bool IsAnticheatTriggered()
        {
            return AnticheatInternal.AnticheatTriggered;
        }
    }
}