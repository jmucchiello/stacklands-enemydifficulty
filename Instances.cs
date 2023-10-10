using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CommonModNS
{
    public static class I
    {
        public static WorldManager WM => WorldManager.instance;
        public static WorldManager.GameState GameState => WM.CurrentGameState;
        public static RunVariables CRV => WM.CurrentRunVariables;
        public static GameDataLoader GDL => WM.GameDataLoader;
        public static PrefabManager PFM => PrefabManager.instance;
        public static GameScreen GS => GameScreen.instance;
        public static ModOptionsScreen MOS => ModOptionsScreen.instance;
        public static ModalScreen Modal => ModalScreen.instance;
        public static void Log(string msg)
        {
            try
            {
                log?.Invoke(null, new object[] { msg });
            }
            catch (Exception) { }
        }
        public static string Xlat(string termId, params LocParam[] terms)
        {
            string xlat = terms.Length > 0 ? SokLoc.Translate(termId, terms) : SokLoc.Translate(termId);
            if (xlat == "---MISSING---")
            {
                Log($"XLAT {termId} {xlat}");
                return null;
            }
            return xlat;
        }

        private static MethodInfo log;

        /**
         * If you declare this in your Mod class:
         *      public static void Log(string msg) => Instance?.Logger.Log(msg);
         * This code will find it and make I.Log call your Log function. And then you can copy this file
         * into any mod and the I.Log function will automatically work with you having to rename how you find the instance to the mod.
         */
        static I()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type myMod = assembly.ExportedTypes.First(x => typeof(Mod).IsAssignableFrom(x));
            log = myMod.GetMethod("Log");
            if (!log.IsStatic) log = null;
        }
    }
}
