using CommonModNS;
using HarmonyLib;
using System.Collections;
using System.Reflection;
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
        static void Postfix(ForestCombatManager __instance)
        {
            if (waveNumber == ForestCombatManager.instance.WickedWitchWave && waveNumber > 10)
            {
                WickedWitch witch = Cutscenes.FindOrCreateWitch();
                SpecialHit hit = witch.ProcessedCombatStats.SpecialHits.Find(x => x.HitType == SpecialHitType.Heal);
                if (hit == null)
                {
                    hit = new SpecialHit() { HitType = SpecialHitType.Heal, Target = SpecialHitTarget.AllFriendly };
                    witch.BaseCombatStats.SpecialHits.Add(hit);
                }
                hit.Chance = Mathf.Clamp((float)waveNumber, 0f, 50f);
                if (waveNumber > 30)
                {
                    SpecialHit hit2 = witch.ProcessedCombatStats.SpecialHits.Find(x => x.HitType == SpecialHitType.Heal);
                    if (hit2 == null)
                    {
                        hit2 = new SpecialHit() { HitType = SpecialHitType.Invulnerable, Target = SpecialHitTarget.AllFriendly };
                        witch.BaseCombatStats.SpecialHits.Add(hit2);
                    }
                    hit2.Chance = Mathf.Clamp((float)(waveNumber - 30), 0f, 50f);
                }
            }
            I.Log($"SpawnWave {waveNumber} Strength {strength}");
        }
    }

    [HarmonyPatch(typeof(ForestCombatManager), "PrepareWave")]
    public class ForestCombatManager_LeaveForest
    {
        static void Prefix()
        {
            if (EnemyDifficultyMod.WitchesRespawn && I.CRV.ForestWave % 10 == 9)
            {
                ForestCombatManager.instance.WickedWitchWave = I.CRV.ForestWave + 1;
                I.CRV.FinishedWickedWitch = false;
            }
        }
    }

    [HarmonyPatch(typeof(ForestCombatManager), "FinishWave")]
    public class ForestCombatManager_FinishWave
    {
        private readonly static MethodInfo miLayoutVillagers = AccessTools.Method(typeof(ForestCombatManager), "LayoutVillagers");
        private readonly static MethodInfo miFocusCameraOn = AccessTools.Method(typeof(Cutscenes), "FocusCameraOnWitchAndVillagers");

        private static bool Prefix(ForestCombatManager __instance)
        {
            ForestCombatManager.DeleteAllCorpses();
            QuestManager.instance.SpecialActionComplete("completed_forest_wave");
            ++I.WM.CurrentRunVariables.ForestWave;
            I.WM.CurrentRunVariables.CanDropItem = true;
            int forestWave = I.CRV.ForestWave;
            __instance.CombatState = ForestCombatState.Finished;
            miLayoutVillagers.Invoke(__instance, [(object)false]);

            WorldManager.instance.QueueCutscene(
                forestWave < 10 ? Cutscenes.ForestWaveEnd() :
                forestWave == 10 ? Cutscenes.ForestLastWaveEnd() :
                forestWave % 10 == 0 ? Cutscene() :
                                Cutscenes.ForestEndlessWaveEnd());
            return false;
        }

        private static IEnumerator Cutscene()
        {
            GameCanvas.instance.SetScreen<CutsceneScreen>();
            GameCamera.instance.CenterOnBoard(WorldManager.instance.GetBoardWithId("forest"));
            Cutscenes.Title = SokLoc.Translate("label_forest_wave_title");
            miFocusCameraOn.Invoke(null, []);
            AudioManager.me.PlaySound2D(ForestCombatManager.instance.WitchSounds, UnityEngine.Random.Range(1.1f, 1.3f), 0.5f);
            WickedWitch witch = Cutscenes.FindOrCreateWitch();
            WorldManager.instance.CreateSmoke(witch.MyGameCard.transform.position);
            Cutscenes.Text = SokLoc.Translate("enemydifficultymod_witch_returns");
            yield return Cutscenes.WaitForContinueClicked(SokLoc.Translate("label_okay"));
            Cutscenes.Text = SokLoc.Translate("label_forest_wave_end2");
            yield return Cutscenes.WaitForAnswer(I.Xlat("label_forest_wave_end_wave_10"), I.Xlat("label_forest_wave_end_leave"));
            if (WorldManager.instance.ContinueButtonIndex == 0)
            {
                WorldManager.instance.CreateSmoke(witch.MyGameCard.transform.position);
                witch.MyGameCard.DestroyCard();
                ForestCombatManager.instance.PrepareWave();
                Cutscenes.Text = SokLoc.Translate("label_forest_fight_wave_10");
                yield return Cutscenes.WaitForContinueClicked(SokLoc.Translate("label_okay"));
                AudioManager.me.PlaySound2D(ForestCombatManager.instance.WitchSounds, UnityEngine.Random.Range(1.1f, 1.3f), 0.5f);
                GameCamera.instance.TargetPositionOverride = null;
                ForestCombatManager.instance.StartWave();
            }
            else
            {
                Cutscenes.Text = SokLoc.Translate("label_forest_leave");
                yield return Cutscenes.WaitForContinueClicked(SokLoc.Translate("label_okay"));
                WorldManager.instance.CreateSmoke(witch.MyGameCard.transform.position);
                witch.MyGameCard.DestroyCard();
                AudioManager.me.PlaySound2D(ForestCombatManager.instance.WitchSounds, UnityEngine.Random.Range(1.1f, 1.3f), 0.5f);
                GameCamera.instance.TargetPositionOverride = null;
                ForestCombatManager.instance.LeaveForest();
            }
            Cutscenes.Text = "";
            Cutscenes.Title = "";
            GameCamera.instance.TargetPositionOverride = null;
            GameCamera.instance.CameraPositionDistanceOverride = null;
            GameCamera.instance.TargetCardOverride = null;
            GameCanvas.instance.SetScreen<GameScreen>();
            I.WM.currentAnimation = null;
        }
    }
}
