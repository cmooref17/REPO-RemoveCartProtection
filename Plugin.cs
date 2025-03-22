using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using REPO_RemoveCartProtection.Config;

namespace REPO_RemoveCartProtection
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        public static Plugin instance;
        private static ManualLogSource logger;

        internal static string[] blacklistedItemNames;

        private void Awake()
        {
            instance = this;
            CreateCustomLogger();
            ConfigSettings.BindConfigSettings();

            blacklistedItemNames = ConfigSettings.ParseBlacklistedItemNames();
            if (blacklistedItemNames != null && blacklistedItemNames.Length > 0)
            {
                Log("Blacklisting " + blacklistedItemNames.Length + " items for Removing Cart Protection: ");
                for (int i = 0; i < blacklistedItemNames.Length; i++)
                    Log(blacklistedItemNames[i]);
            }

            this._harmony = new Harmony(PluginInfo.PLUGIN_NAME);
            PatchAll();
            Log(PluginInfo.PLUGIN_NAME + " loaded");
        }


        private void PatchAll()
        {
            IEnumerable<Type> types;
            try { types = Assembly.GetExecutingAssembly().GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }
            foreach (var type in types)
                this._harmony.PatchAll(type);
        }

        private void CreateCustomLogger()
        {
            try { logger = BepInEx.Logging.Logger.CreateLogSource(string.Format("{0}-{1}", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)); }
            catch { logger = Logger; }
        }

        internal static void Log(string message) => logger.LogInfo(message);
        internal static void LogError(string message) => logger.LogError(message);
        internal static void LogWarning(string message) => logger.LogWarning(message);
        internal static void LogVerbose(string message) { if (ConfigSettings.verboseLogs.Value) logger.LogInfo("[VERBOSE] " + message); }
        internal static void LogErrorVerbose(string message) { if (ConfigSettings.verboseLogs.Value) logger.LogError("[VERBOSE] " + message); }
        internal static void LogWarningVerbose(string message) { if (ConfigSettings.verboseLogs.Value) logger.LogWarning("[VERBOSE] " + message); }
    }
}
