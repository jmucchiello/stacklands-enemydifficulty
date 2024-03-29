﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnemyDifficultyModNS
{
    [HarmonyPatch(typeof(Mob), "CanBeDragged", MethodType.Getter)]
    public class MobsCanBeDragged
    {
        static void Postfix(Mob __instance, ref bool __result)
        {
            if (EnemyDifficultyMod.AllowEnemyDrags && __instance is Enemy)
            {
                if (__instance.MyGameCard.BeingDragged && __instance.MyGameCard.InventoryVisible)
                    __instance.MyGameCard.OpenInventory(false);
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Mimic), "CanBeDragged", MethodType.Getter)]
    public class MimicCanBeDragged
    {
        static void Postfix(Mimic __instance, ref bool __result)
        {
            if (EnemyDifficultyMod.AllowEnemyDrags)
            {
                if (__instance.MyGameCard.BeingDragged && __instance.MyGameCard.InventoryVisible)
                    __instance.MyGameCard.OpenInventory(false);
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(PirateBoat), "CanBeDragged", MethodType.Getter)]
    public class PirateBoatCanBeDragged
    {
        static void Postfix(PirateBoat __instance, ref bool __result)
        {
            if (EnemyDifficultyMod.AllowEnemyDrags)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(StrangePortal), "CanBeDragged", MethodType.Getter)]
    public class StrangePortalCanBeDragged
    {
        static void Postfix(StrangePortal __instance, ref bool __result)
        {
            if (EnemyDifficultyMod.AllowEnemyDrags && !__instance.IsTakingPortal)
            {
                __result = true;
            }
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
