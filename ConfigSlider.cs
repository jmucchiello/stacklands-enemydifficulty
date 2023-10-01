using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EnemyDifficultyModNS
{
    public class ConfigSlider : ConfigEntryHelper
    {
        public static ModLogger Logger;
        public static void Log(string message) => Logger?.Log(message);
        public delegate string SetSliderText(int value);
        public SetSliderText setSliderText;
        public SetSliderText setSliderTooltip;
        public string Text;
        public override object BoxedValue { get => Value; set => Value = (int)value; }

        public int Value { get; set; }
        public int DefaultValue;

        public Action<int> onChange;

        public static GameObject Slider = null;
        GameObject SliderFill = null;
        GameObject SliderText = null;
        Image SliderImage = null;
        public static CustomButton SliderBtn = null;
        public static RectTransform SliderRectTransform = null;
        public static Vector2 SliderSize = Vector2.zero;
        bool ParentIsPopup = false;

        public static int LowerBound = 0, UpperBound = 1, Step = 1;
        public static int Span = 0;

        public static (T,T) Swap<T>(T a, T b) { return (b, a); }
        public static void Swap<T>(ref T a, ref T b) { T c = a; a = b; b = c; }
        /**
         *  Create a header line in the config screen. Also useful for stuff like "Close" in ModalScreen
         **/
        public ConfigSlider(string name, ConfigFile config, string text, Action<int> OnChange, int low, int high, int step = 1, int defValue = 0, bool parentIsPopup = false)
        {
            Name = name;
            Config = config;
            ValueType = typeof(int);
            onChange = OnChange;
            EnemyDifficultyMod.Log($"{name} {parentIsPopup}");
            ParentIsPopup = parentIsPopup;
            if (low > high) (low, high) = Swap(low, high);
            LowerBound = low;
            UpperBound = high;
            Step = step;
            Span = high - low + step;

            DefaultValue = Math.Clamp(defValue, low, high);
            Value = Math.Clamp(LoadConfigEntry<int>(name, defValue), low, high);

            SokTerm term = SokLoc.instance.CurrentLocSet.GetTerm(text ?? name) ?? SokLoc.FallbackSet.GetTerm(text ?? name);
            Text = term?.GetText() ?? text ?? name;
            UI = new ConfigUI()
            {
                Hidden = true,
                OnUI = delegate (ConfigEntryBase c)
                {
                    try
                    {
                        Transform tb = GameScreen.instance.transform.Find("TimeBackground");
                        Log($"{Name} {ParentIsPopup} {tb}");
                        Transform x = UnityEngine.Object.Instantiate(tb);
                        Log($"Slider {x == null}");
                        Slider = x?.gameObject;
                        Log($"Slider {Slider == null}");
                        x.SetParentClean(parentIsPopup ? I.Modal.ButtonParent : I.MOS.ButtonsParent);
                        Slider.name = "SliderBackground" + Name;
                        EnemyDifficultyMod.Log($"Slider {Slider}");
                        for (int i = 0; i < Slider.transform.childCount; ++i)
                        {
                            GameObject goChild = Slider.transform.GetChild(i).gameObject;
                            if (goChild.name == "SpeedIcon") goChild.SetActive(false);
                            if (goChild.name == "TimeFill") SliderFill = goChild;
                            if (goChild.name == "TimeText") SliderText = goChild;
                        }
                    }
                    catch (Exception ex)
                    {
                        EnemyDifficultyMod.Log(ex.ToString());
                    }

                    LayoutElement layout = Slider.GetComponent<LayoutElement>();
                    layout.preferredHeight = -1; // original component has a fixed height

                    SliderRectTransform = Slider.GetComponent<RectTransform>();
                    SliderRectTransform.localScale = Vector3.one;
                    SliderRectTransform.localPosition = Vector3.zero;
                    SliderSize = SliderRectTransform.offsetMax - SliderRectTransform.offsetMin;
#pragma warning disable 8602
                    SliderText.name = "SliderText" + Name;
                    SliderFill.name = "SliderFill" + Name;
#pragma warning restore 8602
                    SliderFill.transform.localScale = Vector3.one;
                    SliderImage = SliderFill.GetComponent<Image>();
                    SliderBtn = Slider.GetComponent<CustomButton>();
                    SliderBtn.name = "SliderButton" + Name;
                    SliderBtn.transform.localScale = Vector3.one;
                    SliderBtn.Clicked += () =>
                    {
                        SliderRectTransform = Slider.GetComponent<RectTransform>();
                        SliderRectTransform.localScale = Vector3.one;
                        SliderRectTransform.localPosition = Vector3.zero;
                        SliderSize = SliderRectTransform.offsetMax - SliderRectTransform.offsetMin;
                        Vector2 pos = InputController.instance.ClampedMousePosition();
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(SliderRectTransform, pos, null, out Vector2 newpos);
                        Vector2 tmp = newpos;
                        float value = tmp.x / SliderSize.x;
                        int OldValue = Value;
                        Value = Math.Clamp((int)(Span * value + step / 2) / step * step + low, low, high);
                        SetSlider();
                        if (OldValue != Value) onChange?.Invoke(Value);
                        Log($"test.clicked called {SliderSize} {pos} {newpos} {value} {Value}");
                    };
                    SetSlider();
                }
            };
            config.Entries.Add(this);
        }

        public void SetSlider()
        {
            SliderImage.fillAmount = (float)(Value - LowerBound) / (float)(UpperBound - LowerBound);
            string btnText = SizeText(36, setSliderText?.Invoke(Value) ?? Text + " <color=blue>" + Value.ToString() + "</color>");
            SliderBtn.TextMeshPro.text = btnText;
//            SliderBtn.TooltipText = setSliderTooltip?.Invoke(Value);
            Log($"Fill Amount {LowerBound} {UpperBound} {Value} {SliderImage.fillAmount}");
            Config.Data[Name] = Value;
        }

        public override void SetDefaults()
        {
            bool change = Value != DefaultValue;
            Value = DefaultValue;
            SetSlider();
            if (change) onChange?.Invoke(Value);
        }
        
    }

    [HarmonyPatch(typeof(CustomButton),"Update")]
    public class CustomButton_Update
    {
        public static void Postfix(CustomButton __instance)
        {
            Vector2 pos = InputController.instance.ClampedMousePosition();
            RectTransform SliderRectTransform = ConfigSlider.Slider?.GetComponent<RectTransform>();
            if (__instance.name == "SliderButtonenemydifficultymod_strength" && 
                SliderRectTransform != null && 
                RectTransformUtility.ScreenPointToLocalPointInRectangle(SliderRectTransform, pos, null, out Vector2 newpos))
            {
                Vector2 SliderSize = SliderRectTransform.offsetMax - SliderRectTransform.offsetMin;
                float value = (newpos.x + SliderSize.x) / SliderSize.x;
                int Value = Math.Clamp((int)(ConfigSlider.Span * value + ConfigSlider.Step / 2) / ConfigSlider.Step * ConfigSlider.Step + ConfigSlider.LowerBound, ConfigSlider.LowerBound, ConfigSlider.UpperBound);
                __instance.TooltipText = $"{Value}%";
//                ConfigSlider.Log($"{newpos} {ConfigSlider.SliderSize} Values {value:0.00}, {Value}, {ConfigSlider.Span} {ConfigSlider.Step} {ConfigSlider.LowerBound} {ConfigSlider.UpperBound} {__instance.name}");
            }
        }
    }
}
