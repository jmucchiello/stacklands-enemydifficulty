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
            SavePatches();  // here
            SetupConfig();  // Settings.cs
            WMCreateCard_Patch.Setup(I.WM);  // Patches.cs
            Harmony.PatchAll();
        }

        public override void Ready()
        {
            saveHelper.Ready(Path);
            ApplyConfig();  // Settings.cs
            Log("Ready!");
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

        public void Notification()
        {
            if (SaveMode != SaveSettingsMode.Disabled)
            {
                I.GS.AddNotification(I.Xlat("enemydifficultymod_notify"),
                                     I.Xlat($"enemydifficultymod_strength_{SaveMode}") +
                                     ": " + ConfigEntryHelper.ColorText(Color.blue, $"{StrengthModifier}%"));
            }
        }
    }
}
