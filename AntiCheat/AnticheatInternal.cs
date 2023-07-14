using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace AntiCheat
{
    public class AnticheatInternal : MelonMod
    {
        internal static bool AnticheatTriggered { get; set; }

        private static readonly MethodInfo DeserializeLevelDataCompressed = typeof(GhostUtils).GetMethod("DeserializeLevelDataCompressed", BindingFlags.NonPublic | BindingFlags.Static);

        public override void OnApplicationLateStart()
        {
            HarmonyLib.Harmony harmony = new("de.MOPSKATER.ac");

            MethodInfo target = typeof(GhostRecorder).GetMethod("GetCompressedSavePath", BindingFlags.NonPublic | BindingFlags.Static);
            HarmonyMethod patch = new(typeof(AnticheatInternal).GetMethod("RedirectGhost"));
            harmony.Patch(target, null, patch);
            
            target = typeof(GhostUtils).GetMethod("LoadLevelDataCompressed");
            patch = new(typeof(AnticheatInternal).GetMethod("LoadCustomGhost"));
            harmony.Patch(target, patch);

            target = typeof(string).GetMethod("Concat", new Type[] { typeof(string[]) });
            patch = new(typeof(AnticheatInternal).GetMethod("PreConcat"));
            harmony.Patch(target, patch);

            // Try to prevent times <= 0

            target = typeof(LeaderboardScoreCalculation).GetMethod("GetGlobalNeonScoreUploadData", new Type[] { typeof(string[]) });
            patch = new(typeof(AnticheatInternal).GetMethod("PreventRushGlobal"));
            harmony.Patch(target, null, patch);

            target = typeof(LeaderboardScoreCalculation).GetMethod("GetLevelRushScoreUploadData", new Type[] { typeof(string[]) });
            patch = new(typeof(AnticheatInternal).GetMethod("PreventRushGlobal"));
            harmony.Patch(target, null, patch);

            target = typeof(LeaderboardScoreCalculation).GetMethod("GetLevelScoreUploadData", new Type[] { typeof(string[]) });
            patch = new(typeof(AnticheatInternal).GetMethod("PreventIL"));
            harmony.Patch(target, null, patch);
        }

        public static void RedirectGhost(ref string __result)
        {
            if (Anticheat.activeMods.Count == 0 || !__result.EndsWith("0.phant")) return;
            __result = __result.Substring(0, __result.Length - 7) + Anticheat.comboName + ".phant";
        }

        public static bool LoadCustomGhost(ref GhostSave ghostSave, ref GhostUtils.GhostType ghostType, ref Action callback)
        {
            if (Anticheat.activeMods.Count == 0) return true;

            ghostSave = new GhostSave();
            string text = "";
            if (!GhostUtils.GetPath(ghostType, ref text))
            {
                return false;
            }
            text = text + Path.DirectorySeparatorChar.ToString() + Anticheat.comboName + ".phant";
            string data = "";
            if (File.Exists(text))
            {
                Debug.LogError(text + " path ");
                data = File.ReadAllText(text);
            }
            try
            {
                DeserializeLevelDataCompressed.Invoke(null, new object[] { ghostSave, data, callback });
            }
            catch
            {
                File.Delete(text);
            }
            callback?.Invoke();
            return false;
        }

        public static void PreConcat(ref string[] values)
        {
            if (Anticheat.activeMods.Count == 0) return;

            if (values.Length == 5 && values[4] == "medallog.txt")
                values[4] = "Medals " + Anticheat.comboName + ".txt";
        }

        public static void PreventRushGlobal(ref int score, ref int scoreParam) => CheckAndKill(score, "Rush or Global");

        public static void PreventIL(ref LevelData levelData, ref int score) => CheckAndKill(score, levelData.levelID);

        private static void CheckAndKill(int score, string message)
        {
            if (score > 0) return;

            Debug.LogError("Illegal score on " + message + " " + score);
            Application.Quit();
        }
    }
}