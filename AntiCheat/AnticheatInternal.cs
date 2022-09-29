using HarmonyLib;
using MelonLoader;
using System.Reflection;

namespace AntiCheat
{
    public class AnticheatInternal : MelonMod
    {
        internal static bool AnticheatTriggered { get; set; }

        public override void OnApplicationLateStart()
        {
            HarmonyLib.Harmony harmony = new("de.MOPSKATER.ac");

            MethodInfo target = typeof(LevelStats).GetMethod("UpdateTimeMicroseconds");
            HarmonyMethod patch = new(typeof(AnticheatInternal).GetMethod("PreventNewScore"));
            harmony.Patch(target, patch);

            target = typeof(Game).GetMethod("OnLevelWin");
            patch = new(typeof(AnticheatInternal).GetMethod("PreventNewGhost"));
            harmony.Patch(target, patch);

            target = typeof(LevelRush).GetMethod("IsCurrentLevelRushScoreBetter", BindingFlags.NonPublic | BindingFlags.Static);
            patch = new(typeof(AnticheatInternal).GetMethod("PreventNewBestLevelRush"));
            harmony.Patch(target, patch);
        }

        public static bool PreventNewScore(LevelStats __instance, ref long newTime)
        {
            if (newTime < __instance._timeBestMicroseconds)
            {
                if (!AnticheatTriggered)
                    __instance._timeBestMicroseconds = newTime;
                else
                    if (__instance._timeBestMicroseconds == 999999999999L)
                    __instance._timeBestMicroseconds = 600000000;
                __instance._newBest = true;
            }
            else
                __instance._newBest = false;
            __instance._timeLastMicroseconds = newTime;
            return false;
        }

        public static bool PreventNewGhost(Game __instance)
        {
            if (AnticheatTriggered)
                __instance.winAction = null;
            return true;
        }

        public static bool PreventNewBestLevelRush(ref bool __result)
        {
            if (!AnticheatTriggered) return true;
            __result = false;
            return false;
        }
    }
}
