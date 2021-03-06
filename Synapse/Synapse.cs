﻿using MEC;
using Synapse.Api;
using Synapse.Api.Plugin;
using Synapse.Config;
using Synapse.Events;
using Synapse.Events.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SynapseModLoader;

namespace Synapse
{
    public static class Synapse
    {
        #region Version
        private const int MajorVersion = 1;
        private const int MinorVersion = 2;
        private const int Patch = 1;

        public static int VersionNumber => MajorVersion * 100 + MinorVersion * 10 + Patch;
        public static string Version => $"{MajorVersion}.{MinorVersion}.{Patch}";
        #endregion

        private static bool _isLoaded;
        private static readonly List<Assembly> LoadedDependencies = new List<Assembly>();
        internal static readonly List<Plugin> plugins = new List<Plugin>(); //TODO: Rework Config System and make this private
        private static EventHandlers _eventHandler;
        public static List<PluginDetails> Plugins => plugins.Select(obj => obj.Details).ToList();


        public static void LoaderExecutionCode()
        {
            if (_isLoaded) return;

            Log.Info($"Now starting Synapse Version {Version}");
            Log.Info("Created by Dimenzio and SirRoob");

            CustomNetworkManager.Modded = true;

            try
            {
                Timing.CallDelayed(0.5f, () => Start());
                _isLoaded = true;
            }
            catch
            {
                Log.Error("Synapse failed to Start.Restart the Server");
            }
        }
        private static void Start()
        {
            LoadDependencies();

            foreach (var plugin in Directory.GetFiles(Files.ServerPluginDirectory))
            {
                if (plugin == "Synapse.dll") continue;

                if (plugin.EndsWith(".dll")) LoadPlugin(plugin);
            }

            HarmonyPatch();
            ConfigManager.InitializeConfigs();
            ServerConsole.ReloadServerName();
            _eventHandler = new EventHandlers();
            try
            {
                PermissionReader.Init();
            }
            catch (Exception e)
            {
                Log.Error($"Your Permission in invalid: {e}");
            }

            OnEnable();
            OnReloadCommands();
        }


        #region Methods to do everything what Synapse needs
        private static void LoadDependencies()
        {
            Log.Info("Loading Dependencies...");
            var depends = Directory.GetFiles(Files.DependenciesDirectory);

            foreach (var dll in depends)
            {
                if (!dll.EndsWith(".dll")) continue;

                if (LoadedDependencies.Any(x => x.Location == dll))
                    return;

                var assembly = Assembly.LoadFrom(dll);
                LoadedDependencies.Add(assembly);
                Log.Info($"Successfully loaded {assembly.GetName().Name}");
            }
        }
        private static void LoadPlugin(string pluginPath)
        {
            Log.Info($"Loading {pluginPath}");
            try
            {
                var file = ModLoader.ReadFile(pluginPath);
                var assembly = Assembly.Load(file);

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract) continue;

                    if (type.FullName == null) continue;

                    if (!typeof(Plugin).IsAssignableFrom(type)) continue;

                    var plugin = Activator.CreateInstance(type);

                    if (!(plugin is Plugin p)) continue;

                    p.Details = type.GetCustomAttribute<PluginDetails>() ?? new PluginDetails
                    {
                        Author = "Unknown",
                        Description = "No Description",
                        Name = assembly.GetName().Name,
                        Version = assembly.ImageRuntimeVersion,
                        SynapseMajor = MajorVersion,
                        SynapseMinor = MinorVersion,
                        SynapsePatch = Patch
                    };

                    p.Assembly = assembly;

                    plugins.Add(p);
                    if (p.Details.SynapseMajor * 10 + p.Details.SynapseMinor == MajorVersion * 10 + MinorVersion) Log.Info($"Successfully loaded {p.Details.Name}");

                    else if (p.Details.SynapseMajor * 10 + p.Details.SynapseMinor > MajorVersion * 10 + MinorVersion) Log.Warn($"The Plugin {p.Details.Name} is for the newer Synapse version {p.Details.GetVersionString()} but was succesfully loaded(bugs can occure)");

                    else Log.Warn($"The Plugin {p.Details.Name} is for the older Synapse version {p.Details.GetVersionString()} but was succesfully loaded(bugs can occure)");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while initializing {pluginPath}: {e}");
            }
        }
        private static void HarmonyPatch()
        {
            try
            {
                var patchHandler = new PatchHandler();
                patchHandler.PatchMethods();
            }
            catch (Exception e)
            {
                Log.Error($"PatchError: {e}");
            }
        }
        private static void OnEnable()
        {
            foreach (var plugin in plugins)
                try
                {
                    plugin.Translation = new Translation { Plugin = plugin };
                    plugin.OwnPluginFolder = Path.Combine(Files.ServerPluginDirectory, plugin.Details.Name);
                    plugin.OnEnable();
                }
                catch (Exception e)
                {
                    Log.Error($"Plugin {plugin.Details.Name} threw an exception while enabling {e}");
                }
        }
        internal static void OnReloadCommands()
        {
            Server.ClientCommandHandler.ClearCommands();
            Server.GameCoreCommandHandler.ClearCommands();
            Server.RaCommandHandler.ClearCommands();

            Server.ClientCommandHandler.LoadGeneratedCommands();
            Server.GameCoreCommandHandler.LoadGeneratedCommands();
            Server.RaCommandHandler.LoadGeneratedCommands();

            foreach (var plugin in plugins)
                try
                {
                    plugin.RegisterCommands();
                }
                catch (Exception e)
                {
                    Log.Error($"Plugin {plugin.Details.Name} threw an exception while enabling {e}");
                }
        }
        #endregion
    }

    internal static class Files
    {
        //Synapse Directory
        private static string StartupFile => Assembly.GetAssembly(typeof(ReferenceHub)).Location.Replace($"SCPSL_Data{Path.DirectorySeparatorChar}Managed{Path.DirectorySeparatorChar}Assembly-CSharp.dll", "SynapseStart-config.yml");
        internal static string SynapseDirectory => new YamlConfig(StartupFile).GetString("synapse_installation", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Synapse"));

        //Directory-Structure
        internal static string DependenciesDirectory
        {
            get
            {
                var path = Path.Combine(SynapseDirectory, "dependencies");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }
        internal static string MainPluginDirectory
        {
            get
            {
                var path = Path.Combine(SynapseDirectory, "Plugins");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }
        internal static string MainConfigDirectory
        {
            get
            {
                var path = Path.Combine(SynapseDirectory, "ServerConfigs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }
        internal static string ServerPluginDirectory
        {
            get
            {
                var path = Path.Combine(MainPluginDirectory, $"Server-{ServerStatic.ServerPort}");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }
        internal static string ServerConfigDirectory
        {
            get
            {
                var path = Path.Combine(MainConfigDirectory, $"Server-{ServerStatic.ServerPort}");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }

        //Files
        internal static string ServerConfigFile
        {
            get
            {
                var path = Path.Combine(ServerConfigDirectory, "server-config.yml");

                if (!File.Exists(path))
                    File.Create(path).Close();

                return path;
            }
        }
        internal static string PermissionFile
        {
            get
            {
                var path = Path.Combine(Files.ServerConfigDirectory, "permissions.yml");

                if (!File.Exists(path))
                    File.WriteAllText(path, "groups:\n    user:\n        default: true\n        permissions:\n        - plugin.permission\n    northwood:\n        northwood: true\n        permissions:\n        - plugin.permission\n    owner:\n        permissions:\n        - .*");

                return path;
            }
        }
    }
}
