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
            //SetupRunopts(); // Runopts.cs
            WMCreateCard_Patch.Setup(I.WM);  // Patches.cs
            Harmony.PatchAll();
        }

        RunoptsSlider runoptsDifficulty;
        
        private void SetupRunopts()
        {
            runoptsDifficulty = new RunoptsSlider("enemydifficultymod_difficulty", 100, 50, 300, 5)
            {
                NameTerm = "enemydifficultymod_runopts",
                TooltipTerm = "enemydifficultymod_strength_tooltip",
                FontColor = Color.blue,
                FontSize = 20,
                Value = configStrength.Value
            };

            HookRunOptions.ApplyPatch(Harmony);
        }

        public override void Ready()
        {
            //saveHelper.Ready(Path);
            NoWitchNoPortal = null;
            if (CheckNoWitchNoPortals())
            {
                configForestWaves.UI.NameTerm = "enemydifficultymod_forest_unavailable";
                configForestWaves.UI.TooltipTerm = "enemydifficultymod_forest_unavailable_tooltip";
                configWitchRespawnEnabled.UI.NameTerm = "enemydifficultymod_forest_unavailable";
                configWitchRespawnEnabled.UI.TooltipTerm = "enemydifficultymod_forest_unavailable_tooltip";
            }
            Config.OnSave.Invoke();
            ApplyConfig();  // Settings.cs
            Log("Ready!");
        }

        private bool? NoWitchNoPortal;
        private bool CheckNoWitchNoPortals()
        {
            if (!NoWitchNoPortal.HasValue)
            {
                NoWitchNoPortal = ModManager.LoadedMods.Find(n => n.Manifest.Id == "NoWitchNoPortal") != null;
            }
            Log($"NoWitchNoPortal {NoWitchNoPortal}");
            return NoWitchNoPortal.Value;
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
            //WorldManagerPatches.LoadSaveRound += WM_OnLoad;
            //WorldManagerPatches.GetSaveRound += WM_OnSave;
            WorldManagerPatches.StartNewRound += WM_OnNewRound;
            WorldManagerPatches.Play += WM_OnPlay;
            WorldManagerPatches.ApplyPatches(Harmony);
        }

        private void WM_OnNewRound(WorldManager wm)
        {
            //SaveMode = configTournament.Value;
            ApplyStrengthMultiplier(InternalPercentage);
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
            I.CRV.ForestWave = 19;
            Log($"Strength {StrengthModifier}; Drag {AllowEnemyDrags}; Forest {ForestMoreDangerous}; Witch {WitchesRespawn}; Notifications {AllowNotifications}; Witch Wave {ForestCombatManager.instance.WickedWitchWave}");
            Notification();
        }

        public void Notification()
        {
            if (AllowNotifications)// SaveMode != SaveSettingsMode.Disabled)
            {
                string text = I.Xlat("enemydifficultymod_strength") +
                                     ": " + ConfigEntryHelper.ColorText(Color.blue, $"{StrengthModifier}%") + ".";
                if (ForestMoreDangerous)
                {
                    text += "\n" + I.Xlat("enemydifficultymod_notify_forest") +
                                     " " + ConfigEntryHelper.ColorText(Color.blue, I.Xlat(configForestWaves.Value ? "label_on" : "label_off")) + ".";
                    if (WitchesRespawn)
                    {
                        text += "\n" + I.Xlat("enemydifficultymod_notify_witches") +
                                     " " + ConfigEntryHelper.ColorText(Color.blue, I.Xlat(configWitchRespawnEnabled.Value ? "label_on" : "label_off")) + ".";
                    }
                }
                I.GS.AddNotification(I.Xlat("enemydifficultymod_notify"), text);
            }
        }
    }
}
