using CommonModNS;
using HarmonyLib;
using UnityEngine;

namespace EnemyDifficultyModNS
{
    public partial class EnemyDifficultyMod : Mod
    {
        public void ApplyStrengthMultiplier(int percentage)
        {
            EmemySpawning_Patch.StrengthMultiplier = Math.Clamp(percentage, 50f, 300f) / 100f;
            Log($"Enemy Strength Multiplier: {EmemySpawning_Patch.StrengthMultiplier}");
        }
    }

    [HarmonyPatch(typeof(SpawnHelper),nameof(SpawnHelper.GetEnemiesToSpawn))]
    [HarmonyPatch(new Type[] { typeof(List<SetCardBagType>), typeof(float), typeof(bool) })]
    internal class EmemySpawning_Patch
    {
        public static float StrengthMultiplier = 1.0f;

        static void Prefix(List<CardIdWithEquipment> __result, List<SetCardBagType> cardbags, ref float strength, bool canHaveInventory)
        {
            if (ForestCombatManager_SpawnWave.inForest)
            {
                strength = ForestCombatManager_SpawnWave.strength * StrengthMultiplier;
            }
            else
            {
                strength *= StrengthMultiplier;
            }
        }
    }

    [HarmonyPatch(typeof(WorldManager),nameof(WorldManager.CreateCard))]
    [HarmonyPatch(new Type[] { typeof(Vector3), typeof(CardData), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public class WMCreateCard_Patch
    {
        private static Traverse<bool> WM_IsLoadingSaveRound = null;

        public static void Setup(WorldManager wm)
        {
            if (WM_IsLoadingSaveRound == null)
                WM_IsLoadingSaveRound = new Traverse(wm).Field<bool>("IsLoadingSaveRound");
        }

        static void Postfix(WorldManager __instance, CardData __result) //, ref Vector3 position)
        {
            bool loading = WM_IsLoadingSaveRound?.Value ?? true;
            if (!loading && __result is Combatable c && __result is not BaseVillager)
            {
                if (__result.Id == Cards.wicked_witch && ForestCombatManager_SpawnWave.inForest && EnemyDifficultyMod.WitchesRespawn)
                {
                    c.BaseCombatStats.MaxHealth *= ForestCombatManager_SpawnWave.waveNumber / 10;
                }
                c.BaseCombatStats.MaxHealth = (int)(c.BaseCombatStats.MaxHealth * EmemySpawning_Patch.StrengthMultiplier);
                c.HealthPoints = c.ProcessedCombatStats.MaxHealth;
            }
        }
    }

    [HarmonyPatch(typeof(ForestCombatManager),"SpawnWave")]
    public class ForestCombatManager_SpawnWave
    {
        public static bool inForest = false;
        public static float strength = 0f;
        public static int waveNumber = 0;

        static void Prefix(ForestCombatManager __instance, int wave)
        {
            inForest = EnemyDifficultyMod.ForestMoreDangerous;
            strength = 10 * wave + 10;
            waveNumber = wave;
            I.Log($"SpawnWave {waveNumber} Strength {strength}");
        }
    }

    [HarmonyPatch(typeof(ForestCombatManager), "PrepareWave")]
    public class ForestCombatManager_LeaveForest
    {
        static void Prefix()
        {
            if (EnemyDifficultyMod.WitchesRespawn && WorldManager.instance.CurrentRunVariables.ForestWave == ForestCombatManager.instance.WickedWitchWave + 10)
            {
                ForestCombatManager.instance.WickedWitchWave += 10;
                I.CRV.FinishedWickedWitch = false;
            }
        }
    }
}
