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
        private ConfigTournament configTournament;
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

            configTournament = new ConfigTournament("enemydifficultymod_tournament", Config);

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
