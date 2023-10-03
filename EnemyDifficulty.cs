﻿using EnemyDifficultyModNS;
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

        public ConfigSlider configStrength;

        private void Awake()
        {
            instance = this;
            WMCreateCard_Patch.WM_IsLoadingSaveRound = new Traverse(I.WM).Field<bool>("IsLoadingSaveRound");
            ConfigSlider.Logger = instance.Logger;
            SetupConfig();
            Harmony.PatchAll();
        }

        private void SetupConfig()
        {
            ConfigFreeText freeText = new ConfigFreeText("none", Config, "enemydifficultymod_strength", "enemydifficultymod_strength_tooltip");
            freeText.TextAlign = TextAlign.Center;
            configStrength = new ConfigSlider("enemydifficultymod_strength", Config, "enemydifficultymod_strength", OnChangeStrength, 50, 300, 10, 100);
            configStrength.setSliderText = delegate (int value)
            {
                string txt = ConfigEntryHelper.CenterAlign(ConfigEntryHelper.ColorText(Color.blue, $"{value}%"));
                return txt;
            };
            CustomButton_Update.sliders.Add("SliderButtonenemydifficultymod_strength", configStrength);
            ConfigFreeText resetDefault = new ConfigFreeText("none", Config, "enemydifficultymod_reset");
            resetDefault.TextAlign = TextAlign.Right;
            resetDefault.Clicked += delegate (ConfigEntryBase _, CustomButton _)
            {
                configStrength?.SetDefaults();
            };
            Config.OnSave = delegate ()
            {
                ApplyConfig();
            };
        }

        private void OnChangeStrength(int value)
        {

        }

        private void ApplyConfig()
        {
            ApplyStrengthMultiplier();
        }

        public override void Ready()
        {
            ApplyConfig();
            Logger.Log("Ready!");
        }

        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.LoadSaveRound))]
        [HarmonyPostfix]
        static void WorldManager_LoadSaveRound(WorldManager __instance, SaveRound saveRound)
        {
            instance.ApplyConfig();
            I.GS.AddNotification(I.Xlat("enemydifficultymod_notify"),
                                 I.Xlat("enemydifficultymod_strength") + $": " + ConfigEntryHelper.ColorText(Color.blue, $"{ instance.configStrength.Value}%"));
        }
    }
}