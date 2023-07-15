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
        private static readonly MethodInfo ParseLevelTotalTimeCompressed = typeof(GhostUtils).GetMethod("ParseLevelTotalTimeCompressed", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo SaveCompressedInternal = typeof(GhostRecorder).GetMethod("SaveCompressedInternal", BindingFlags.NonPublic | BindingFlags.Static);

        public override void OnApplicationLateStart()
        {
            HarmonyLib.Harmony harmony = new("de.MOPSKATER.ac");

            MethodInfo target = typeof(GhostRecorder).GetMethod("SaveCompressed", BindingFlags.Public | BindingFlags.Static);
            HarmonyMethod patch = new(typeof(AnticheatInternal).GetMethod("PreSaveCompressed"));
            harmony.Patch(target, patch);

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

        public static bool PreSaveCompressed(ref GhostFrame[] framesToSave, ref int index, ref string forcedPath, ref ulong forcedID, ref bool saveToTemp)
        {
            if (Anticheat.activeMods.Count == 0 || forcedID > 0) return true;

            float totalTime = framesToSave[index - 1].time;
            if (totalTime >= Singleton<Game>.Instance.GetCurrentLevel().GetTimeSilver() * 2f) return false;

            string path = "";
            GhostUtils.GetPath(GhostUtils.GhostType.PersonalGhost, ref path);
            if (forcedPath != "")
                path = forcedPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = path + Path.DirectorySeparatorChar.ToString() + Anticheat.comboName + ".phant";

            bool pathPreExists = File.Exists(path);
            bool saveToTempAndInEditor = false;
            if (pathPreExists)
            {
                float obj = float.MaxValue;
                string text = "";
                if (GhostUtils.GetPath(GhostUtils.GhostType.PersonalGhost, ref text))
                {
                    text = text + Path.DirectorySeparatorChar.ToString() + Anticheat.comboName + ".phant";
                    if (File.Exists(text))
                        obj = (float) ParseLevelTotalTimeCompressed.Invoke(null, new object[] { File.ReadAllText(text) });
                }

                Debug.LogError(obj.ToString() + "    " + totalTime.ToString());
                bool replaceCurrentRecording = (obj > totalTime);
                if (replaceCurrentRecording | saveToTempAndInEditor)
                    SaveCompressedInternal.Invoke(null, new object[] { framesToSave, index, forcedPath, forcedID, saveToTemp, replaceCurrentRecording, totalTime, path, pathPreExists });
                return false;
            }
            SaveCompressedInternal.Invoke(null, new object[] { framesToSave, index, forcedPath, forcedID, saveToTemp, true, totalTime, path, pathPreExists });
            return false;
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