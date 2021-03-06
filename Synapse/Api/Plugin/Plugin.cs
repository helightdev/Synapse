﻿using System;
using System.IO;
using System.Reflection;

using CommandSystem;

namespace Synapse.Api.Plugin
{
    public abstract class Plugin
    {
        internal Assembly Assembly;
        /// <summary>
        ///     The Main Config from the current Server which all Plugins can use
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public static YamlConfig Config;

        [Obsolete("Please use public override void ReloadConfigs() now!")]
        public delegate void OnConfigReload();
        [Obsolete("Please use public override void ReloadConfigs() now!")]
        public event OnConfigReload ConfigReloadEvent;

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Translation Translation { get; internal set; }

        public PluginDetails Details { get; internal set; }

        private string _ownPluginFolder;

        /// <summary>
        ///     A Directory especially for your Plugin which are created by Synapse for you!
        /// </summary>
        /// <remarks>The Name of the Directory is based on the GetName string from your Plugin!</remarks>
        public string OwnPluginFolder
        {
            // ReSharper disable once UnusedMember.Global
            get
            {
                if (!Directory.Exists(_ownPluginFolder))
                    Directory.CreateDirectory(_ownPluginFolder);

                return _ownPluginFolder + Path.DirectorySeparatorChar;
            }
            internal set => _ownPluginFolder = value;
        }

        /// <summary>The Method ist always activated when the Server starts</summary>
        /// <remarks>You can use it to hook Events</remarks>
        public abstract void OnEnable();

        public virtual void RegisterCommands()
        {
            foreach(var type in Assembly.GetTypes())
            {
                if (type.GetInterface("ICommand") != typeof(ICommand)) continue;

                if (!Attribute.IsDefined(type, typeof(CommandHandlerAttribute))) continue;

                foreach(var attributeData in type.CustomAttributes)
                {
                    try
                    {
                        if (attributeData.AttributeType != typeof(CommandHandlerAttribute)) continue;

                        var cmdType = (Type)attributeData.ConstructorArguments[0].Value;

                        var cmd = (ICommand)Activator.CreateInstance(type);

                        if (cmdType == typeof(RemoteAdminCommandHandler))
                            Server.RaCommandHandler.RegisterCommand(cmd);

                        if (cmdType == typeof(GameConsoleCommandHandler))
                            Server.GameCoreCommandHandler.RegisterCommand(cmd);

                        if (cmdType == typeof(ClientCommandHandler))
                            Server.ClientCommandHandler.RegisterCommand(cmd);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error occured while registering a command: {e}");
                    }
                }
            }
        }

        public virtual void ReloadConfigs() => ConfigReloadEvent.Invoke();
    }
}