using EnemyDifficultyModNS;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace EnemyDifficultyModNS
{
    public partial class EnemyDifficultyMod : Mod
    {
        public static EnemyDifficultyMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);

        public ConfigSlider configStrength;

        private void Awake()
        {
            instance = this;
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
//                string txt = I.Xlat("enemydifficultymod_strength") + ": " + ConfigEntryHelper.ColorText(Color.blue, $"{value}%");
                string txt = ConfigEntryHelper.CenterAlign(ConfigEntryHelper.ColorText(Color.blue, $"{value}%"));
                return txt;
            };
            ConfigFreeText resetDefault = new ConfigFreeText("none", Config, "enemydifficultymod_reset");
            resetDefault.TextAlign = TextAlign.Right;
            resetDefault.Clicked += delegate (ConfigEntryBase _, CustomButton _)
            {
                configStrength?.SetDefaults();
            };
        }

        private void OnChangeStrength(int value)
        {

        }

        private void ApplyConfig()
        {

        }

        public override void Ready()
        {
            Logger.Log("Ready!");
        }
    }
}