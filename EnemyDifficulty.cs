using CommonModNS;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;


namespace EnemyDifficultyModNS
{
    public enum SaveLockStatus { Unknown, Locked, Nolock, Broken }

    [HarmonyPatch]
    public partial class EnemyDifficultyMod : Mod
    {
        public static EnemyDifficultyMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);

        public static float StrengthModifer { get; private set; } // set in ApplyConfig()

        public static int StrengthPercentage => instance?.configStrength?.Value ?? 100;
        public static bool StrModIsLocked { get; set; } = false;
        public static SaveLockStatus SaveLockStatus { get; set; } = SaveLockStatus.Unknown;

        private ConfigSlider configStrength;
        private ConfigEntryBool configTournament;

        private void Awake()
        {
            instance = this;
            WorldManagerPatches.LoadSaveRound += WM_OnLoad;
            WorldManagerPatches.GetSaveRound += WM_OnSave;
            WorldManagerPatches.ApplyPatches(Harmony);
            WMCreateCard_Patch.WM_IsLoadingSaveRound = new Traverse(I.WM).Field<bool>("IsLoadingSaveRound");
            SetupConfig();
            Harmony.PatchAll();
        }

        private void SetupConfig()
        {
            ConfigFreeText freeText = new ConfigFreeText("none", Config, "enemydifficultymod_strength", "enemydifficultymod_strength_tooltip")
            {
                TextAlign = TextAlign.Center
            };
            configStrength = new ConfigSlider("enemydifficultymod_strength", Config, OnChangeStrength, 50, 300, 10, 100);
            
            configTournament = new ConfigEntryBool("enemydifficultymod_tournament", Config, false)
            {
                onDisplayText = delegate { return I.Xlat("enemydifficultymod_tournament") + ": " + ConfigEntryHelper.ColorText(Color.blue, I.Xlat(configTournament.Value ? "label_on" : "label_off")); },
                onDisplayTooltip = delegate { return I.Xlat("enemydifficultymod_tournament_tooltip"); }
            };

            ConfigFreeText resetDefault = new ConfigFreeText("none", Config, "enemydifficultymod_reset");
            resetDefault.TextAlign = TextAlign.Right;
            resetDefault.Clicked += delegate (ConfigEntryBase _, CustomButton _)
            {
                configStrength?.SetDefaults();
                configTournament?.SetDefaults();
            };
            Config.OnSave = delegate ()
            {
                ApplyConfig();
            };
        }

        private void OnChangeStrength(int value) { }

        private void ApplyConfig()
        {
            ApplyStrengthMultiplier();
            StrModIsLocked = SaveLockStatus == SaveLockStatus.Broken || (configTournament?.Value ?? false);
        }

        public override void Ready()
        {
            ApplyConfig();
            Log("Ready!");
        }

        public void WM_OnSave(WorldManager wm, SaveRound saveRound)
        {
            string value = !StrModIsLocked || SaveLockStatus == SaveLockStatus.Nolock ? "NOLOCK" :
                           SaveLockStatus == SaveLockStatus.Broken ? "BROKEN" : $"{StrengthPercentage}";
            Log($"SaveConfig - {value}");
            saveRound.ExtraKeyValues.SetOrAdd("enemydifficultymod_lock", value);
        }

        public void WM_OnLoad(WorldManager wm, SaveRound saveRound)
        {
            ApplyConfig();
            string value = saveRound.ExtraKeyValues.Find(x => x.Key == "enemydifficultymod_lock")?.Value;
            if (value == null)
            {
                Log("LoadConfig - value was null");
                StrModIsLocked = SaveLockStatus != SaveLockStatus.Broken && configTournament.Value;
                SaveLockStatus = SaveLockStatus == SaveLockStatus.Broken ? SaveLockStatus.Broken : configTournament.Value ? SaveLockStatus.Locked : SaveLockStatus.Nolock;
                return;
            }

            Log($"LoadConfig - {value}");
            if (value == "BROKEN")
            {
                StrModIsLocked = true;
                SaveLockStatus = SaveLockStatus.Broken;
            }
            else if (value == "NOLOCK")
            {
                StrModIsLocked = false;
                SaveLockStatus = SaveLockStatus.Nolock;
            }
            else if (Int32.TryParse(value, out int strength))
            {
                StrModIsLocked = true;
                SaveLockStatus = SaveLockStatus.Locked;
                EmemySpawning_Patch.SpawnMultiplier = StrengthModifer = Math.Clamp(strength, 50f, 300f) / 100f; // override config
            }
            else
            {
                StrModIsLocked = true;
                SaveLockStatus = SaveLockStatus.Broken;
            }
            instance.Notification();
        }

        public void Notification()
        {
            I.GS.AddNotification(I.Xlat("enemydifficultymod_notify"),
                                 I.Xlat(StrModIsLocked ? "enemydifficultymod_strengthlocked" : "enemydifficultymod_strength") +
                                 ": " + ConfigEntryHelper.ColorText(Color.blue, $"{EmemySpawning_Patch.SpawnMultiplier * 100}%"));
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