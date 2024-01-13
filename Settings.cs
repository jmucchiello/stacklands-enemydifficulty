using CommonModNS;
using UnityEngine;

namespace EnemyDifficultyModNS
{
    public partial class EnemyDifficultyMod
    {
        public static int StrengthModifier { get => (int)((EmemySpawning_Patch.StrengthMultiplier + 1f/200f) * 100f); }  // use this when dealing with what is "current" or in the save files
        public static int StrengthPercentage { get => instance?.configStrength?.Value ?? 100; } // always the value from Mod Options Screen
        public static SaveSettingsMode SaveMode { get; set; } = SaveSettingsMode.Casual;
        public static bool ForestMoreDangerous { get => instance?.configForestWaves?.Value ?? false; }
        public static bool WitchesRespawn { get => instance?.configWitchRespawnEnabled?.Value ?? false; }
        public static bool AllowEnemyDrags { get => instance?.configDraggableMobs.Value ?? false; }
        public static bool AllowNotifications { get => instance?.configNotifications.Value ?? false; }  

        private static int InternalPercentage { get; set; }

        private ConfigSlider configStrength;
        private ConfigEntryBool configForestWaves;
        private ConfigEntryBool configWitchRespawnEnabled;
        private ConfigEntryBool configDraggableMobs;
        private ConfigEntryBool configNotifications;
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

            configDraggableMobs = new ConfigEntryBool("enemydifficultymod_dragmobs", Config, false, new ConfigUI()
            {
                NameTerm = "enemydifficultymod_dragmobs",
                TooltipTerm = "enemydifficultymod_dragmobs_tooltip"
            })
            {
                currentValueColor = Color.blue,
                FontSize = 25
            };

            _ = new ConfigFreeText("", Config, "enemydifficultymod_config_forest");

            configForestWaves = new ConfigEntryBool("enemydifficultymod_forest", Config, false, new ConfigUI()
            {
                NameTerm = CheckNoWitchNoPortals() ? "enemydifficultymod_forest_unavailable" : "enemydifficultymod_forest",
                TooltipTerm = CheckNoWitchNoPortals() ? "enemydifficultymod_forest_unavailable_tooltip" : "enemydifficultymod_forest_tooltip"
            }) {
                currentValueColor = Color.blue,
                FontSize = 25,
                OnChange = delegate {
                    if (CheckNoWitchNoPortals()) return configForestWaves.Value = false;
                    return true;
                }
            };

            configWitchRespawnEnabled = new ConfigEntryBool("enemydifficultymod_witch", Config, false, new ConfigUI() {
                NameTerm = "enemydifficultymod_witch",
                TooltipTerm = "enemydifficultymod_witch_tooltip"
            } ) {
                currentValueColor = Color.blue,
                FontSize = 25,
                OnChange = delegate {
                    if (CheckNoWitchNoPortals()) return configWitchRespawnEnabled.Value = false;
                    return true;
                }
            };

            _ = new ConfigFreeText("", Config, "enemydifficultymod_config_mod");

            configNotifications = new ConfigEntryBool("enemydifficultymod_notifications", Config, true, new ConfigUI()
            {
                NameTerm = "enemydifficultymod_notifications",
                TooltipTerm = "enemydifficultymod_notifications_tooltip"
            } ) {
                currentValueColor = Color.blue,
                FontSize = 25
            };


            ConfigResetDefaults resetDefault = new ConfigResetDefaults(Config, () => {
                configStrength?.SetDefaults();
                configForestWaves?.SetDefaults();
                configWitchRespawnEnabled?.SetDefaults();
                configDraggableMobs?.SetDefaults();
            });

            //_ = new ConfigClearSave(saveHelper, Config);

            Config.OnSave += delegate ()
            {
                InternalPercentage = configStrength.Value;
                Log($"Config.OnSave() {InternalPercentage}");
                ApplyConfig();
            };
        }

        private void ApplyConfig()
        {
            NoWitchNoPortal = null;
            if (CheckNoWitchNoPortals())
            {
                configForestWaves.Value = false;
                configWitchRespawnEnabled.Value = false;
            }

            ApplyStrengthMultiplier(InternalPercentage);
        }
    }
}
