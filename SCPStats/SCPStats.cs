// -----------------------------------------------------------------------
// <copyright file="SCPStats.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using SCPStats.Patches;
using SCPStats.Websocket;

namespace SCPStats
{
    public class SCPStats
    {
        internal static readonly string EXILED_CONFIG_PATH =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EXILED", "Configs",
                "SCPStats");
        
        internal static SCPStats Singleton;

        internal string ServerID = "fill this";
        internal string Secret = "fill this";

        [PluginConfig]
        public Config Config;
        [PluginConfig("translations.yml")]
        public Translation Translation;

        internal string ID = "";

        private static Harmony _harmony;

        private CoroutineHandle _update;
        private CoroutineHandle _requests;
        private CoroutineHandle _cache;

        [PluginPriority(LoadPriority.Lowest)]
        [PluginEntryPoint("SCPStats", "1.6.0", "Tracks player stats and provides server management utilities.", "PintTheDragon")]
        public void OnEnabled()
        {
            Singleton = this;
            
            LoadConfigs();

            if (Secret == "fill this" || ServerID == "fill this")
            {
                for (var i = 0; i < 10; i++)
                {
                    Log.Error("Config for SCPStats has not been filled out correctly. Disabling!");
                }

                Log.Error(
                    "Go to https://docs.scpstats.com for more information on how to setup SCPStats.");

                PluginHandler.Get(this).Unload();
                return;
            }
            
            EventManager.RegisterEvents<EventHandler>(this);

            _harmony = new Harmony($"SCPStats-{DateTime.Now.Ticks}");
            _harmony.PatchAll();
            
            EventHandler.Start();
            
            if (Config.AutoUpdates)
            {
                AutoUpdater.RunUpdater(10000);
                _update = Timing.RunCoroutine(AutoUpdates());
            }

            EventHandler.LoadLocalBanCache();
            Timing.RunCoroutine(EventHandler.UpdateLocalBanCache());
            _cache = Timing.RunCoroutine(UpdateCache());

            _requests = Timing.RunCoroutine(WebsocketRequests.DequeueRequests());

            EventHandler.OnRAReload();
            Timing.CallDelayed(5f, EventHandler.OnRAReload);

            Timing.CallDelayed(5f, () =>
            {
                WebsocketHandler.SendRequest(RequestType.SetVersion, AutoUpdater.Version);
            });
        }

        private void LoadConfigs()
        {
            if (Singleton == null) return;

            // We're going to try reading from the NWAPI path.
            // As a fallback, we'll read from EXILED, and then create the NWAPI files.
            var path = PluginHandler.Get(this).PluginDirectoryPath;
            
            var serverIdPath = Path.Combine(path, "ServerID.txt");
            var secretPath = Path.Combine(path, "Secret.txt");

            string serverId;
            string secret;
            
            if (!File.Exists(serverIdPath))
            {
                var exiledServerIdPath = Path.Combine(EXILED_CONFIG_PATH, Server.Port + "-ServerID.txt");
                
                serverId = !File.Exists(exiledServerIdPath) ? "fill this" : File.ReadAllText(exiledServerIdPath);
                File.WriteAllText(serverIdPath, serverId);
            }
            else serverId = File.ReadAllText(serverIdPath);

            if (!File.Exists(secretPath))
            {
                var exiledSecretPath = Path.Combine(EXILED_CONFIG_PATH, Server.Port + "-Secret.txt");
                
                secret = !File.Exists(exiledSecretPath) ? "fill this" : File.ReadAllText(exiledSecretPath);
                File.WriteAllText(secretPath, secret);
            }
            else secret = File.ReadAllText(secretPath);

            serverId = serverId.Trim().ToLower();
            secret = secret.Trim().ToLower();

            if (serverId.Length != 18 && serverId != "fill this")
            {
                File.WriteAllText(serverIdPath, "fill this");
                ServerID = "fill this";
            }
            else ServerID = serverId;

            if (secret.Length != 32 && secret != "fill this")
            {
                File.WriteAllText(secretPath, "fill this");
                Secret = "fill this";
            }
            else Secret = secret;
        }

        [PluginUnload]
        public void OnDisabled()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;

            Timing.KillCoroutines(_update, _cache, _requests);
            
            EventManager.UnregisterEvents<EventHandler>(this);

            EventHandler.Reset();
            Hats.Hats.Reset();
            Integrations.ClearReflection();

            UnbanPatch.LastId = null;

            ServerID = null;
            Secret = null;
            Singleton = null;
        }

        private void OnReloadedTranslations()
        {
            // TODO: Use.
            //this.OnUnregisteringCommands();
            //this.OnRegisteringCommands();
        }

        private IEnumerator<float> AutoUpdates()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(7200);

                AutoUpdater.RunUpdater(10000);
            }
        }

        private IEnumerator<float> UpdateCache()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(3600);

                Timing.RunCoroutine(EventHandler.UpdateLocalBanCache());
            }
        }
    }
}