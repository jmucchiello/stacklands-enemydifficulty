using CommonModNS;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;

namespace EnemyDifficultyModNS
{
    public partial class EnemyDifficultyMod : Mod
    {
        public void ApplyStrengthMultiplier()
        {
            EmemySpawning_Patch.SpawnMultiplier = StrengthModifer = (float)StrengthPercentage / 100f;
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
            string s = String.Join(",", cardbags.ToArray());
            float originalStrength = strength;
            strength *= SpawnMultiplier;
//            EnemyDifficultyMod.Log($"SpawnHelper.GetEnemiesToSpawn - {originalStrength:F02} modified strength {strength:F02} {SpawnMultiplier}");
        }
    }

    [HarmonyPatch(typeof(WorldManager),nameof(WorldManager.CreateCard))]
    [HarmonyPatch(new Type[] { typeof(Vector3), typeof(CardData), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public class WMCreateCard_Patch
    {
        public static Traverse<bool> WM_IsLoadingSaveRound;
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

#if false
    [HarmonyPatch(typeof(Crab),nameof(Crab.Die))]
    internal class MommaCrab_Patch
    {
        public static int MommaCrabFrequency = 3;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> result = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldc_I4_3)
                )
                .Set(OpCodes.Ldsfld, AccessTools.Field(typeof(MommaCrab_Patch), "MommaCrabFrequency"))
                .InstructionEnumeration()
                .ToList();
            return result;
        }
    }
#endif
}
