﻿using Mirror;
using System;
using System.Reflection;

namespace Synapse.Api
{
    public static class Server
    {
        private static MethodInfo _sendSpawnMessage;
        private static Broadcast broadcast;
        private static BanPlayer banPlayer;


        public static Player Host => Player.Host;

        public static string Name
        {
            get => ServerConsole._serverName;
            set
            {
                ServerConsole._serverName = value;
                ServerConsole.singleton.RefreshServerName();
            }
        }

        public static ushort Port { get => ServerStatic.ServerPort; set => ServerStatic.ServerPort = value; }

        public static MethodInfo SendSpawnMessage
        {
            get
            {
                if (_sendSpawnMessage == null)
                    _sendSpawnMessage = typeof(NetworkServer).GetMethod("SendSpawnMessage", BindingFlags.Instance | BindingFlags.InvokeMethod
                        | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);

                return _sendSpawnMessage;
            }
        }

        public static Broadcast Broadcast
        {
            get
            {
                if (broadcast == null)
                    broadcast = Host.GetComponent<Broadcast>();

                return broadcast;
            }
        }

        public static BanPlayer BanPlayer
        {
            get
            {
                if (banPlayer == null)
                    banPlayer = Host.GetComponent<BanPlayer>();

                return banPlayer;
            }
        }

        public static ServerConsole Console => ServerConsole.singleton;


        public static int GetMethodHash(Type invokeClass, string methodName) => invokeClass.FullName.GetStableHashCode() * 503 + methodName.GetStableHashCode();
    }
}
