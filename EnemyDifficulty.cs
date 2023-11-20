using CommonModNS;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;


namespace EnemyDifficultyModNS
{
    [HarmonyPatch]
    public partial class EnemyDifficultyMod : Mod
    {
        public static EnemyDifficultyMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);

        private void Awake()
        {
            instance = this;
            SetupConfig();  // Settings.cs
            SavePatches();  // here
            WMCreateCard_Patch.Setup(I.WM);  // Patches.cs
            Harmony.PatchAll();
        }

        public override void Ready()
        {
            saveHelper.Ready(Path);
            ApplyConfig();  // Settings.cs
            Log("Ready!");
        }

        public void Notification()
        {
            if (SaveMode != SaveSettingsMode.Disabled)
            {
                I.GS.AddNotification(I.Xlat("enemydifficultymod_notify"),
                                     I.Xlat($"enemydifficultymod_strength_{SaveMode}") +
                                     ": " + ConfigEntryHelper.ColorText(Color.blue, $"{StrengthModifier}%"));
            }
        }

        private SaveHelper saveHelper;

        private void SavePatches()
        {
            saveHelper = new SaveHelper("EnemyDifficultyMod")
            {
                onGetSettings = delegate ()
                {
                    return StrengthPercentage.ToString();
                }
            };
            WorldManagerPatches.LoadSaveRound += WM_OnLoad;
            WorldManagerPatches.GetSaveRound += WM_OnSave;
            WorldManagerPatches.StartNewRound += WM_OnNewRound;
            WorldManagerPatches.Play += WM_OnPlay;
            WorldManagerPatches.ApplyPatches(Harmony);
        }

        private void WM_OnNewRound(WorldManager wm)
        {
            SaveMode = configTournament.Value;
            ApplyStrengthMultiplier(StrengthPercentage);
        }

        private void WM_OnSave(WorldManager wm, SaveRound saveRound)
        {
            saveHelper.SaveData(saveRound, SaveMode);
        }

        private void WM_OnLoad(WorldManager wm, SaveRound saveRound)
        {
            (SaveMode, string payload) = saveHelper.LoadData(saveRound);
            if (SaveMode == SaveSettingsMode.Tournament)
            {
                if (!Int32.TryParse(payload, out int percentage)) percentage = 100;
                InternalPercentage = percentage;
            }
            else if (SaveMode == SaveSettingsMode.Disabled)
            {
                InternalPercentage = 100;
            }
            else
            {
                InternalPercentage = StrengthPercentage;
            }

            ApplyConfig();
        }

        private void WM_OnPlay(WorldManager wm)
        {
            Notification();
        }
    }
}

#if false
        private void Fun()
        {
            Transform tb = GameCanvas.instance.transform.Find("ModDisablingScreen/Background/Scroll View/Viewport/Content/Buttons");
            for (int i = 0; i < tb.childCount; ++i) 
            {
                CustomButton cb = tb.GetChild(i).GetComponent<CustomButton>();
                if (cb.TextMeshPro.text.Contains("BetterSaves"))
                {
                    cb.TooltipText = "Hey, just so you're aware, if you disable Better Saves, you won't be able to access some of your saves.";
                }
            }
        }


        public static IEnumerator CutSceneLockStrength()
        {
            GameCanvas.instance.SetScreen<CutsceneScreen>();
            Cutscenes.Title = I.Xlat("enemydifficultymod_locktitle");
            Cutscenes.Text = I.Xlat("enemydifficultymod_locktext", LocParam.Create("amount", StrengthPercentage.ToString()));
            yield return Cutscenes.WaitForAnswer(I.Xlat("enemydifficultymod_lock"),
                                                 I.Xlat("enemydifficultymod_nolock"),
                                                 I.Xlat("enemydifficultymod_defer"));
            if (I.WM.ContinueButtonIndex == 0)
            {
                SaveLockStatus = SaveLockStatus.Locked;
            }
            else if (I.WM.ContinueButtonIndex == 1)
            {
                SaveLockStatus = SaveLockStatus.Nolock;
            }
            else if (I.WM.ContinueButtonIndex == 2)
            {
                SaveLockStatus = SaveLockStatus.Deferred;
            }
            instance.Notification();

            Cutscenes.Text = "";
            Cutscenes.Title = "";
            GameCanvas.instance.SetScreen<GameScreen>();
            I.WM.currentAnimation = null;
        }
#endif