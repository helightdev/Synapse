﻿using System;
using Synapse.Api.Plugin;

namespace Synapse.Config
{
    public static class ConfigManager
    {
        internal static void InitializeConfigs()
        {
            Plugin.Config = new YamlConfig(Files.ServerConfigFile);
            SynapseConfigs.ReloadConfig();
        }

        internal static void ReloadAllConfigs()
        {
            Plugin.Config = new YamlConfig(Files.ServerConfigFile);
            SynapseConfigs.ReloadConfig();

            foreach (var plugin in Synapse.plugins)
                try
                {
                    plugin.ReloadConfigs();
                }
                catch (Exception e)
                {
                    Log.Error($"Plugin {plugin.Details.Name} threw an exception while reloading {e}");
                }
        }
    }
}
