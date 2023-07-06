namespace AntiCheat
{
    public class Anticheat
    {
        internal static string comboName = "";
        internal static readonly List<string> activeMods = new(10);

        public static void Register(string modname)
        {
            activeMods.Add(modname);
            comboName = string.Join("_", activeMods);
        }

        public static void TriggerAnticheat()
        {
            GameDataManager.powerPrefs.dontUploadToLeaderboard = true;
            GS.ToggleSavingAllowed(false);
            AnticheatInternal.AnticheatTriggered = true;
        }

        public static bool IsAnticheatTriggered()
        {
            return AnticheatInternal.AnticheatTriggered;
        }
    }
}