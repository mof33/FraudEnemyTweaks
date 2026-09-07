using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace FraudEnemyTweaks
{
    [BepInPlugin("com.mel33.fraudenemytweaks", "FraudEnemyTweaks", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony("com.mel33.fraudenemytweaks");

            _harmony.PatchAll();

            Log.LogInfo("UKEnemyTweaks loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier), "Start")]
    public static class PowerHealthPatch
    {
        private static void Postfix(EnemyIdentifier __instance)
        {
            if (__instance.enemyType == EnemyType.Power)
            {
                __instance.machine.health = 30f;
                __instance.ForceGetHealth();
            }
        }
    }

    [HarmonyPatch(typeof(Power), "Update")]
    public static class PowerCheapShotPatch
    {
        public static readonly Dictionary<Power, float> damageTakenInWindow = new Dictionary<Power, float>();
        public static readonly Dictionary<Power, float> windowStartTime = new Dictionary<Power, float>();
        public static readonly Dictionary<Power, float> lastHealth = new Dictionary<Power, float>();

        private static void Prefix(Power __instance)
        {
            var cooldowns = MonoSingleton<EnemyCooldowns>.Instance;

            if (cooldowns.attackingPower == __instance || cooldowns.powers.Count <= 1)
                return;

            if (__instance.inAction)
            {
                lastHealth[__instance] = __instance.mach.health;
                return;
            }

            float currentHealth = __instance.mach.health;

            if (!lastHealth.ContainsKey(__instance))
            {
                lastHealth[__instance] = currentHealth;
                windowStartTime[__instance] = Time.time;
                damageTakenInWindow[__instance] = 0f;
                return;
            }

            float damage = lastHealth[__instance] - currentHealth;
            lastHealth[__instance] = currentHealth;

            if (damage > 0f)
            {
                if (!windowStartTime.ContainsKey(__instance))
                    windowStartTime[__instance] = Time.time;

                damageTakenInWindow.TryGetValue(__instance, out float accumulated);
                damageTakenInWindow[__instance] = accumulated + damage;
            }

            if (windowStartTime.TryGetValue(__instance, out float startTime) &&
                Time.time - startTime > 2f)
            {
                damageTakenInWindow[__instance] = 0f;
                windowStartTime[__instance] = Time.time;
            }
        }
    }

    [HarmonyPatch(typeof(Power), nameof(Power.Throw))]
    public static class PowerThrowPatch
    {
        private static bool Prefix(Power __instance, bool cheapShot)
        {
            if (!cheapShot)
                return true;

            if (PowerCheapShotPatch.damageTakenInWindow.TryGetValue(
                    __instance, out float damage) &&
                damage >= 3.5f)
            {
                PowerCheapShotPatch.damageTakenInWindow[__instance] = 0f;
                PowerCheapShotPatch.windowStartTime[__instance] = Time.time;

                return true;
            }

            return false;
        }
    }


    [HarmonyPatch(typeof(MirrorReaper), nameof(MirrorReaper.ProjectileBarrage))]
    public class MirrorReaperCooldownPatch
    {
        private static float lastProjectileBarrageTime;

        [HarmonyPrefix]
        public static bool ProjectileBarragePrefix(MirrorReaper __instance)
        {
            float time = Time.time;
            if (__instance.difficulty == 0 && time - lastProjectileBarrageTime < 16f)
            {
                return false;
            }
            if (__instance.difficulty == 1 && time - lastProjectileBarrageTime < 12f)
            {
                return false;
            }
            if (__instance.difficulty == 2 && time - lastProjectileBarrageTime < 8f)
            {
                return false;
            }
            if (__instance.difficulty == 3 && time - lastProjectileBarrageTime < 6f)
            {
                return false;
            }
            if (__instance.difficulty == 4 && time - lastProjectileBarrageTime < 4f)
            {
                return false;
            }
            lastProjectileBarrageTime = time;
            return true;
        }
    }
}