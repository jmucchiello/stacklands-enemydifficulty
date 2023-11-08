using CommonModNS;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;


namespace EnemyDifficultyModNS
{
    public enum SettingsMode { Casual, Tournament, Disabled, Tampered }

    [HarmonyPatch]
    public partial class EnemyDifficultyMod : Mod
    {
        public static EnemyDifficultyMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);

        public static float StrengthModifier { get => EmemySpawning_Patch.SpawnMultiplier; private set => EmemySpawning_Patch.SpawnMultiplier = value; }

        public static int StrengthPercentage { get => instance?.configStrength?.Value ?? 100; } // always the value from Mod Options Screen
        public static SettingsMode TournamentMode { get => instance?.configTournament?.Value ?? SettingsMode.Casual; } // always the value from Mod Options Screen
        public static SettingsMode SettingsMode { get; set; } = SettingsMode.Casual;

        private ConfigSlider configStrength;
//        private ConfigEntryBool configTournament;
        private ConfigToggledEnum<SettingsMode> configTournament;
        public ConfigFreeText clearCurrentSave;

        private void Awake()
        {
            instance = this;
            SetupConfig();
            WorldManagerPatches.LoadSaveRound += WM_OnLoad;
            WorldManagerPatches.GetSaveRound += WM_OnSave;
            WorldManagerPatches.StartNewRound += WM_OnNewRound;
            WorldManagerPatches.Play += WM_OnPlay;
            WorldManagerPatches.ApplyPatches(Harmony);
            WMCreateCard_Patch.Setup();
            Harmony.PatchAll();
        }

        private void SetupConfig()
        {
            ConfigFreeText freeText = new ConfigFreeText("none", Config, "enemydifficultymod_strength", "enemydifficultymod_strength_tooltip")
            {
                TextAlign = TextAlign.Center
            };
            configStrength = new ConfigSlider("enemydifficultymod_strength", Config, OnChangeStrength, 50, 300, 10, 100);

            configTournament = new ConfigToggledEnum<SettingsMode>("enemydifficultymod_tournament", Config, SettingsMode.Casual, new ConfigUI()
            {
                NameTerm = "enemydifficultymod_tournament",
                TooltipTerm = "enemydifficultymod_tournament_tooltip"
            }){
                currentValueColor = Color.blue,
                onChange = delegate (SettingsMode value) {
                    if (value == SettingsMode.Tampered)
                    {
                        configTournament.Value = SettingsMode.Casual;
                        return false;
                    }
                    return true;
                },
                onDisplayEnumText = delegate (SettingsMode value)
                {
                    return I.Xlat($"enemydifficultymod_settings_{value}");
                }
            };

            ConfigFreeText resetDefault = new ConfigFreeText("none", Config, "enemydifficultymod_reset", "enemydifficultymod_reset_tooltip")
            {
                TextAlign = TextAlign.Right
            };
            resetDefault.Clicked += delegate (ConfigEntryBase _, CustomButton _) {
                configStrength?.SetDefaults();
                configTournament?.SetDefaults();
            };

            clearCurrentSave = new ConfigFreeText("clear", Config, "enemydifficultymod_clearsave", "enemydifficultymod_clearsave_tooltip") {
                TextAlign = TextAlign.Left,
                OnUI = delegate(ConfigFreeText ccs, CustomButton _)
                {
                    ccs.Text = SaveHelper.DescribeCurrentSave();
                }
            };
            clearCurrentSave.Clicked += delegate (ConfigEntryBase _, CustomButton _) {
                SaveHelper.ClearCurrentSave();
                clearCurrentSave.UI.NameTerm = "enemydifficultymod_savecleared";
            };
            Config.OnSave = delegate ()
            {
                ApplyConfig();
            };
        }

        private void OnChangeStrength(int value) { }

        private void ApplyConfig()
        {
            ApplyStrengthMultiplier(SettingsMode == SettingsMode.Disabled ? 100 : StrengthPercentage);
        }

        public override void Ready()
        {
            ApplyConfig();
            Log("Ready!");
        }

        public void Notification()
        {
            if (SettingsMode != SettingsMode.Disabled)
            {
                I.GS.AddNotification(I.Xlat("enemydifficultymod_notify"),
                                     I.Xlat($"enemydifficultymod_strength_{SettingsMode}") +
                                     ": " + ConfigEntryHelper.ColorText(Color.blue, $"{StrengthModifier * 100}%"));
            }
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