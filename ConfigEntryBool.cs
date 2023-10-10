using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CommonModNS
{
    public class ConfigEntryBool : ConfigEntryHelper 
    {
        private bool content = true; // access via BoxedValue
        private bool DefaultValue; // access via BoxedValue
        private CustomButton anchor;  // this holds the ModOptionsScreen text that is clicked to open the menu

        public delegate string OnDisplayText();
        public OnDisplayText onDisplayText;
        public OnDisplayText onDisplayTooltip;

        public delegate bool OnChangeCall(bool newValue); // return false to prevent acceptance of newValue
        public OnChangeCall OnChange;

        public Color currentValueColor = Color.black;

        public bool Value {
            get {
                return content;
            }
            set
            {
                content = value;
            }
        }

        public override object BoxedValue
        {
            get => content;
            set => content = (bool)value;
        }

        public ConfigEntryBool(string name, ConfigFile configFile, bool defaultValue, ConfigUI ui = null)
        {
            Name = name;
            DefaultValue = defaultValue;
            ValueType = typeof(bool);
            Config = configFile;
            Value = LoadConfigEntry<bool>(name, defaultValue);

            UI = new ConfigUI()
            {
                Hidden = true,
                Name = ui?.Name ?? name,
                NameTerm = ui?.NameTerm ?? name,
                Tooltip = ui?.Tooltip,
                TooltipTerm = ui?.TooltipTerm,
                PlaceholderText = ui?.PlaceholderText,
                RestartAfterChange = ui?.RestartAfterChange ?? false,
                ExtraData = ui?.ExtraData,
                OnUI = delegate (ConfigEntryBase c)
                {
                    anchor = DefaultButton(I.MOS.ButtonsParent,
                                           GetDisplayText(),
                                           GetDisplayTooltip());
                    anchor.Clicked += delegate
                    {
                        if (OnChange?.Invoke(!Value) ?? true)
                        {
                            Value = !Value;
                        }
                        Config.Data[Name] = Value;
                        Update();
                    };
                }
            };
            configFile.Entries.Add(this);
        }

        public void Update()
        {
            anchor.TextMeshPro.text = GetDisplayText();
            anchor.TooltipText = GetDisplayTooltip();
        }

        public virtual string GetDisplayText()
        {
            return onDisplayText?.Invoke() ?? UI.GetName()
                 + ": "
                 + ColorText(currentValueColor, I.Xlat(content ? "label_on" : "label_off"));
        }
        public virtual string GetDisplayTooltip()
        {
            return onDisplayTooltip?.Invoke() ?? UI.GetTooltip();
        }
        public override void SetDefaults()
        {
            Value = DefaultValue;
            anchor.TextMeshPro.text = GetDisplayText();
            anchor.TooltipText = GetDisplayTooltip();
        }
    }
}
