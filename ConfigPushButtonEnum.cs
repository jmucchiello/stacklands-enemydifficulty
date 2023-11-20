using CommonModNS;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnemyDifficultyModNS
{
    public class ConfigPushButtonEnum<T> : ConfigEntryHelper where T : Enum
    {
        private int content; // access via BoxedValue
        private int defaultValue; // access via BoxedValue
        private string[] EnumNames = new string[0];
//        private CustomButton anchor;  // this holds the ModOptionsScreen text that is clicked to open the menu

        public delegate string OnDisplayText();
        public delegate string OnDisplayEnumText(T t);
        public OnDisplayText onDisplayText;
        public OnDisplayText onDisplayTooltip;
        public OnDisplayEnumText onDisplayEnumText;


        public virtual T DefaultValue { get => (T)(object)defaultValue; set => defaultValue = (int)(object)value; }
        public virtual T Value { get => (T)(object)content; set => content = (int)(object)value; }

        public override object BoxedValue
        {
            get => (T)(object)content;
            set => content = (int)value;
        }

    }
}