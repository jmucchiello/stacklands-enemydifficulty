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
        public static bool ForestMoreDangerous { get => instance?.configForestWaves?.Value ?? false; }
        public static bool WitchMoreDangerous { get => instance?.configWitchRespawnEnabled?.Value ?? false; }

        private static int InternalPercentage { get; set; }

        private ConfigSlider configStrength;
        private ConfigEntryBool configForestWaves;
        private ConfigEntryBool configWitchRespawnEnabled;
        //private ConfigClearSave clearCurrentSave;

        private void SetupConfig()
        {
            configStrength = new ConfigSlider("enemydifficultymod_strength", Config, 50, 300, 5, 100)
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

            configForestWaves = new ConfigEntryBool("enemydifficultymod_forest", Config, false, new ConfigUI()
            {
                NameTerm = "enemydifficultymod_forest",
                TooltipTerm = "enemydifficultymod_forest_tooltip"
            });

            configWitchRespawnEnabled = new ConfigEntryBool("enemydifficultymod_witch", Config, false, new ConfigUI() {
                NameTerm = "enemydifficultymod_witch",
                TooltipTerm = "enemydifficultymod_witch_tooltip"
            });



            ConfigResetDefaults resetDefault = new ConfigResetDefaults(Config, () => {
                configStrength?.SetDefaults();
                configTournament?.SetDefaults();
            });

            _ = new ConfigClearSave(saveHelper, Config);

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
    }
}
