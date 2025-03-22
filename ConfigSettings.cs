using BepInEx.Configuration;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace REPO_RemoveCartProtection.Config
{
    [Serializable]
    public static class ConfigSettings
    {
        public static ConfigEntry<bool> keepImpactProtectionExtraction;
        public static ConfigEntry<float> cartImpactMultiplier;
        public static ConfigEntry<string> removeProtectionBlacklist;
        public static ConfigEntry<bool> verboseLogs;
        public static Dictionary<string, ConfigEntryBase> currentConfigEntries = new Dictionary<string, ConfigEntryBase>();

        internal static void BindConfigSettings()
        {
            Plugin.Log("Binding Configs");

            keepImpactProtectionExtraction = AddConfigEntry(Plugin.instance.Config.Bind("General", "Keep Impact Protection In Extraction", true, new ConfigDescription("Keeps the vanilla impact protection from objects in the extraction zone.")));
            removeProtectionBlacklist = AddConfigEntry(Plugin.instance.Config.Bind("General", "Remove Impact Protection - Blacklist", "Music Box,Frog", new ConfigDescription("Add item names here to prevent removing impact protection from. Separate each item with a \",\" character. Example: \"ItemName1,ItemName2\"\nItems in this blacklist will behaving normally when placing them into the cart.\nNOTE: Item names are a little funky to reference, so at this time, the formatting will be generous and may not need to match 100% of the item name, but may have issues in strange scenarios. Will refine in the future.")));
            cartImpactMultiplier = AddConfigEntry(Plugin.instance.Config.Bind("General", "Cart Impact Multiplier", 0.5f, new ConfigDescription("Lower Value = More Generous. A value of 0.5 will halve the impact when placing items into the cart, decreasing the likelihood of damaging an item. And a value of 1.0 will not reduce the impact at all, so be careful!", new AcceptableValueRange<float>(0.1f, 2.0f))));
            verboseLogs = AddConfigEntry(Plugin.instance.Config.Bind("General", "Verbose Logs", false, new ConfigDescription("Enables verbose logs. Useful for debugging.")));

            cartImpactMultiplier.Value = Mathf.Clamp(cartImpactMultiplier.Value, 0.1f, 2.0f);
        }


        internal static ConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
        {
            currentConfigEntries.Add(configEntry.Definition.Key, configEntry);
            return configEntry;
        }


        internal static string[] ParseBlacklistedItemNames()
        {
            if (removeProtectionBlacklist == null || string.IsNullOrEmpty(removeProtectionBlacklist.Value) || removeProtectionBlacklist.Value.Length <= 0)
                return new string[0];

            string rawString = removeProtectionBlacklist.Value.ToLower();
            rawString = rawString.Replace(" ", "");

            string[] itemNames = rawString.Split(',');
            return itemNames;
        }
    }
}