using CommonModNS;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EnemyDifficultyModNS
{
    public partial class EnemyDifficultyMod
    {
        public static int StrengthModifier { get => (int)((EmemySpawning_Patch.SpawnMultiplier + 1f/200f) * 100f); }  // use this when dealing with what is "current" or in the save files
        public static int StrengthPercentage { get => instance?.configStrength?.Value ?? 100; } // always the value from Mod Options Screen
        public static SaveSettingsMode SaveMode { get; set; } = SaveSettingsMode.Casual;
        private static int InternalPercentage { get; set; }

        private ConfigSlider configStrength;
        private ConfigToggledEnum<SaveSettingsMode> configTournament;
        public ConfigFreeText clearCurrentSave;

        private void SetupConfig()
        {
//            ConfigFreeText freeText = new ConfigFreeText("none", Config, "enemydifficultymod_strength", "enemydifficultymod_strength_tooltip")
//            {
//                TextAlign = TextAlign.Center
//            };
            configStrength = new ConfigSlider("enemydifficultymod_strength", Config, OnChangeStrength, 50, 300, 5, 100)
            {
                currentValueColor = Color.blue,
                HeadingTerm = "enemydifficultymod_strength",
                TooltipTerm = "enemydifficultymod_strength_tooltip",
                onQuickButton = delegate (string text)
                {
                    int value = int.Parse(text[..^1]); // remove % sign
                    configStrength.Value = value;
                }
            };
            configStrength.QuickButtons.AddRange(new List<string> { "75%", "100%", "125%", "150%", "200%" });
                
            configTournament = new ConfigToggledEnum<SaveSettingsMode>("enemydifficultymod_tournament", Config, SaveSettingsMode.Casual, new ConfigUI()
            {
                NameTerm = "enemydifficultymod_tournament",
                TooltipTerm = "enemydifficultymod_tournament_tooltip"
            })
            {
                currentValueColor = Color.blue,
                onChange = delegate (SaveSettingsMode value) {
                    if (value == SaveSettingsMode.Tampered)
                    {
                        configTournament.Value = (SaveSettingsMode)0;
                        return false;
                    }
                    return true;
                },
                onDisplayText = delegate ()
                {
                    return ConfigEntryHelper.SizeText(25, I.Xlat("enemydifficultymod_tournament"));
                },
                onDisplayEnumText = delegate (SaveSettingsMode value)
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

            clearCurrentSave = new ConfigFreeText("clear", Config, "enemydifficultymod_clearsave", "enemydifficultymod_clearsave_tooltip")
            {
                TextAlign = TextAlign.Left,
                OnUI = delegate (ConfigFreeText ccs, CustomButton _)
                {
                    ccs.Text = ConfigEntryHelper.SizeText(20, saveHelper.DescribeCurrentSave());
                }
            };
            clearCurrentSave.Clicked += delegate (ConfigEntryBase _, CustomButton _) {
                I.Modal.Clear();
                I.Modal.SetTexts(I.Xlat("enemydifficultymod_modal_title"), I.Xlat("enemydifficultymod_modal_text"));
                I.Modal.AddOption(I.Xlat(SokTerms.label_yes), () =>
                {
                    GameCanvas.instance.CloseModal();
                    saveHelper.ClearCurrentSave();
                    clearCurrentSave.Update();
                });
                I.Modal.AddOption(I.Xlat(SokTerms.label_no), () =>
                {
                    GameCanvas.instance.CloseModal();
                });
                GameCanvas.instance.OpenModal();
            };

            Config.OnSave = delegate ()
            {
                InternalPercentage = configStrength.Value;
                ApplyConfig();
            };
        }

        private void ApplyConfig()
        {
            ApplyStrengthMultiplier(InternalPercentage);
        }

        private void OnChangeStrength(int value) { }
    }
}
