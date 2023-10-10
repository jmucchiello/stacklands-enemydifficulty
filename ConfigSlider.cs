using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using CommonModNS;

namespace CommonModNS
{
    public class ConfigSlider : ConfigEntryHelper
    {
        public delegate string SetSliderText(int value);
        public SetSliderText setSliderText;
        public SetSliderText setSliderTooltip;
        public string Text;
        public override object BoxedValue { get => Value; set => Value = (int)value; }

        public int Value { get; set; }
        public readonly int DefaultValue;

        public Action<int> onChange;

        public GameObject Slider = null;
        GameObject SliderFill = null;
        GameObject SliderText = null;
        Image SliderImage = null;
        public CustomButton SliderBtn = null;
        public RectTransform SliderRectTransform = null;
        public Vector2 SliderSize = Vector2.zero;
        bool ParentIsPopup = false;

        public int LowerBound = 0, UpperBound = 1, Step = 1;
        public int Span = 0;


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
            I.Log($"{name} {parentIsPopup}");
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
                        I.Log($"{Name} {ParentIsPopup} {tb}");
                        Transform x = UnityEngine.Object.Instantiate(tb);
                        I.Log($"Slider {x == null}");
                        Slider = x?.gameObject;
                        I.Log($"Slider {Slider == null}");
                        x.SetParentClean(parentIsPopup ? I.Modal.ButtonParent : I.MOS.ButtonsParent);
#pragma warning disable 8602
                        Slider.name = "SliderBackground" + Name;
#pragma warning restore 8602
                        I.Log($"Slider {Slider}");
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
                        I.Log(ex.ToString());
                    }

#pragma warning disable 8602
                    LayoutElement layout = Slider.GetComponent<LayoutElement>();
#pragma warning restore 8602
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
                        Vector2 globalMousePos = InputController.instance.ClampedMousePosition();
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(SliderRectTransform, globalMousePos, null, out Vector2 localMousePos);
                        Vector2 tmp = localMousePos;
                        float rawValue = tmp.x / SliderSize.x;
                        int oldValue = Value;
                        Value = Math.Clamp((int)(Span * rawValue + Step / 2) / Step * Step + LowerBound, LowerBound, UpperBound);
                        UpdateSlider();
                        if (oldValue != Value) onChange?.Invoke(Value);
                        I.Log($"test.clicked called {SliderSize} {globalMousePos} {localMousePos} {rawValue} {Value}");
                    };
                    UpdateSlider();
                }
            };
            config.Entries.Add(this);
        }

        public void UpdateSlider()
        {
            SliderImage.fillAmount = (float)(Value - LowerBound) / (float)(UpperBound - LowerBound);
            string btnText = SizeText(36, setSliderText?.Invoke(Value) ?? Text + ": <color=blue>" + Value.ToString() + "</color>");
            SliderBtn.TextMeshPro.text = btnText;
//            SliderBtn.TooltipText = setSliderTooltip?.Invoke(Value); // handled in CustomButton_Update patch
            I.Log($"Fill Amount {LowerBound} {UpperBound} {Value} {SliderImage.fillAmount}");
            Config.Data[Name] = Value;
        }

        public override void SetDefaults()
        {
            bool change = Value != DefaultValue;
            Value = DefaultValue;
            UpdateSlider();
            if (change) onChange?.Invoke(Value);
        }
        
    }

    [HarmonyPatch(typeof(CustomButton),"Update")]
    public class CustomButton_Update
    {
        public static Dictionary<string, ConfigSlider> sliders = new();
        public static void Postfix(CustomButton __instance)
        {
            if (sliders.TryGetValue(__instance.name, out ConfigSlider ConfigSlider))
            {
//                I.Log($"CustomButton_Update {__instance.name} {mousedown}");
                RectTransform SliderRectTransform = ConfigSlider.Slider?.GetComponent<RectTransform>();
                if (SliderRectTransform != null)
                {
                    Vector2 globalMousePos = InputController.instance.ClampedMousePosition();
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(SliderRectTransform, globalMousePos, null, out Vector2 localMousePos))
                    {
                        Vector2 SliderSize = SliderRectTransform.offsetMax - SliderRectTransform.offsetMin;
                        float value = (localMousePos.x + SliderSize.x) / SliderSize.x;
                        int Value = Math.Clamp((int)(ConfigSlider.Span * value + ConfigSlider.Step / 2) / ConfigSlider.Step * ConfigSlider.Step + ConfigSlider.LowerBound, ConfigSlider.LowerBound, ConfigSlider.UpperBound);
                        __instance.TooltipText = $"{Value}%";
                        if (InputController.instance.MouseIsDragging)
                        {
                            ConfigSlider.Value = Value;
                            ConfigSlider.UpdateSlider();
                        }
//                        I.Log($"{mousedown} {localMousePos} {ConfigSlider.SliderSize} Values {value:0.00}, {Value}, {ConfigSlider.Span} {ConfigSlider.Step} {ConfigSlider.LowerBound} {ConfigSlider.UpperBound} {__instance.name}");
                    }
                }
            }
        }
    }
}
