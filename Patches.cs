using CommonModNS;
using HarmonyLib;
using UnityEngine;

namespace EnemyDifficultyModNS
{
    public partial class EnemyDifficultyMod : Mod
    {
        public void ApplyStrengthMultiplier(int percentage)
        {
            EmemySpawning_Patch.SpawnMultiplier = Math.Clamp(percentage, 50f, 300f) / 100f;
            Log($"Spawned Enemies Strength Multiplier: {EmemySpawning_Patch.SpawnMultiplier}");
        }
    }

    [HarmonyPatch(typeof(SpawnHelper),nameof(SpawnHelper.GetEnemiesToSpawn))]
    [HarmonyPatch(new Type[] { typeof(List<SetCardBagType>), typeof(float), typeof(bool) })]
    internal class EmemySpawning_Patch
    {
        public static float SpawnMultiplier = 1.0f;

        static void Prefix(List<CardIdWithEquipment> __result, List<SetCardBagType> cardbags, ref float strength, bool canHaveInventory)
        {
            strength *= SpawnMultiplier;
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
                c.BaseCombatStats.MaxHealth = (int)(c.BaseCombatStats.MaxHealth * EmemySpawning_Patch.SpawnMultiplier);
                c.HealthPoints = c.ProcessedCombatStats.MaxHealth;
            }
        }
    }
}
