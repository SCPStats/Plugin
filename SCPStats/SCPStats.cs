// -----------------------------------------------------------------------
// <copyright file="SCPStats.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using SCPStats.Patches;
using SCPStats.Websocket;

namespace SCPStats
{
    public class SCPStats : Plugin<Config, Translation>
    {
        public override string Name { get; } = "SCPStats";
        public override string Author { get; } = "PintTheDragon";
        public override Version Version { get; } = new Version(1, 6, 0);
        public override Version RequiredExiledVersion { get; } = new Version(6, 0, 0);
        public override bool IgnoreRequiredVersionCheck { get; } = true;
        public override PluginPriority Priority { get; } = PluginPriority.Last;
        public override string Prefix { get; } = "scp_stats";

        internal static SCPStats Singleton;

        internal static string ServerID = "fill this";
        internal static string Secret = "fill this";

        internal string ID = "";

        private static Harmony harmony;

        private CoroutineHandle update;
        private CoroutineHandle requests;
        private CoroutineHandle cache;

        public override void OnEnabled()
        {
            Singleton = this;
            
            LoadConfigs();

            if (Secret == "fill this" || ServerID == "fill this")
            {
                for (var i = 0; i < 10; i++)
                {
                    Log.Error("Config for SCPStats has not been filled out correctly. Disabling!");
                }

                Log.Info(
                    "Go to https://docs.scpstats.com for more information on how to setup SCPStats.");

                this.OnDisabled();
                return;
            }

            harmony = new Harmony($"SCPStats-{Version}-{DateTime.Now.Ticks}");
            harmony.PatchAll();
            
            EventHandler.Start();

            Timing.RunCoroutine(EnableEvents());

            if (Config.AutoUpdates)
            {
                AutoUpdater.RunUpdater(10000);
                update = Timing.RunCoroutine(AutoUpdates());
            }

            EventHandler.LoadLocalBanCache();
            Timing.RunCoroutine(EventHandler.UpdateLocalBanCache());
            cache = Timing.RunCoroutine(UpdateCache());

            requests = Timing.RunCoroutine(WebsocketRequests.DequeueRequests());

            EventHandler.OnRAReload();
            Timing.CallDelayed(5f, EventHandler.OnRAReload);

            Timing.CallDelayed(5f, () =>
            {
                WebsocketHandler.SendRequest(RequestType.SetVersion, AutoUpdater.Version);
            });

            base.OnEnabled();
        }

        private IEnumerator<float> EnableEvents()
        {
            yield return Timing.WaitForSeconds(1f);

            if (Singleton == null) yield break;
            
            Integrations.SetupReflection();
            
            Exiled.Events.Handlers.Server.RoundStarted += EventHandler.OnRoundStart;
            Exiled.Events.Handlers.Server.EndingRound += EventHandler.OnRoundEnding;
            Exiled.Events.Handlers.Server.RestartingRound += EventHandler.OnRoundRestart;
            Exiled.Events.Handlers.Server.WaitingForPlayers += EventHandler.Waiting;
            Exiled.Events.Handlers.Player.Dying += EventHandler.OnKill;
            Exiled.Events.Handlers.Player.ChangingRole += EventHandler.OnRoleChanged;
            Exiled.Events.Handlers.Player.PickingUpItem += EventHandler.OnPickup;
            Exiled.Events.Handlers.Player.DroppingItem += EventHandler.OnDrop;
            Exiled.Events.Handlers.Player.Verified += EventHandler.OnJoin;
            Exiled.Events.Handlers.Player.Destroying += EventHandler.OnLeave;
            Exiled.Events.Handlers.Player.UsedItem += EventHandler.OnUse;
            Exiled.Events.Handlers.Player.ThrownItem += EventHandler.OnThrow;
            Exiled.Events.Handlers.Server.ReloadedRA += EventHandler.OnRAReload;
            Exiled.Events.Handlers.Server.ReloadedConfigs += LoadConfigs;
            Exiled.Events.Handlers.Scp914.UpgradingPickup += EventHandler.OnUpgrade;
            Exiled.Events.Handlers.Player.EnteringPocketDimension += EventHandler.OnEnterPocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += EventHandler.OnEscapingPocketDimension;
            Exiled.Events.Handlers.Player.Banned += EventHandler.OnBan;
            Exiled.Events.Handlers.Player.Kicking += EventHandler.OnKick;
            Exiled.Events.Handlers.Scp049.FinishingRecall += EventHandler.OnRecalling;
            Exiled.Events.Handlers.Player.ReservedSlot += EventHandler.OnReservedSlotCheck;
            Exiled.Events.Handlers.Player.PreAuthenticating += EventHandler.OnPreauth;
            Exiled.Events.Handlers.Server.ReportingCheater += EventHandler.OnReportingCheater;
            Exiled.Events.Handlers.Server.LocalReporting += EventHandler.OnReporting;
            Exiled.Events.Handlers.Server.ReloadedTranslations += this.OnReloadedTranslations;
        }

        private static void LoadConfigs()
        {
            if (Singleton == null) return;

            if (Paths.Configs == null) return;

            var path = Path.Combine(Paths.Configs, "SCPStats");
            Directory.CreateDirectory(path);

            var serverIdPath = Path.Combine(path, Server.Port + "-ServerID.txt");
            var secretPath = Path.Combine(path, Server.Port + "-Secret.txt");

            string serverId;
            string secret;
            
            if (!File.Exists(serverIdPath))
            {
                File.WriteAllText(serverIdPath, "fill this");
                serverId = "fill this";
            }
            else serverId = File.ReadAllText(serverIdPath);

            if (!File.Exists(secretPath))
            {
                File.WriteAllText(secretPath, "fill this");
                secret = "fill this";
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

        public override void OnDisabled()
        {
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;

            Timing.KillCoroutines(update, cache, requests);

            Exiled.Events.Handlers.Server.RoundStarted -= EventHandler.OnRoundStart;
            Exiled.Events.Handlers.Server.EndingRound -= EventHandler.OnRoundEnding;
            Exiled.Events.Handlers.Server.RestartingRound -= EventHandler.OnRoundRestart;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= EventHandler.Waiting;
            Exiled.Events.Handlers.Player.Dying -= EventHandler.OnKill;
            Exiled.Events.Handlers.Player.ChangingRole -= EventHandler.OnRoleChanged;
            Exiled.Events.Handlers.Player.PickingUpItem -= EventHandler.OnPickup;
            Exiled.Events.Handlers.Player.DroppingItem -= EventHandler.OnDrop;
            Exiled.Events.Handlers.Player.Verified -= EventHandler.OnJoin;
            Exiled.Events.Handlers.Player.Destroying -= EventHandler.OnLeave;
            Exiled.Events.Handlers.Player.UsedItem -= EventHandler.OnUse;
            Exiled.Events.Handlers.Player.ThrownItem -= EventHandler.OnThrow;
            Exiled.Events.Handlers.Server.ReloadedRA -= EventHandler.OnRAReload;
            Exiled.Events.Handlers.Server.ReloadedConfigs -= LoadConfigs;
            Exiled.Events.Handlers.Scp914.UpgradingPickup -= EventHandler.OnUpgrade;
            Exiled.Events.Handlers.Player.EnteringPocketDimension -= EventHandler.OnEnterPocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= EventHandler.OnEscapingPocketDimension;
            Exiled.Events.Handlers.Player.Banned -= EventHandler.OnBan;
            Exiled.Events.Handlers.Player.Kicking -= EventHandler.OnKick;
            Exiled.Events.Handlers.Scp049.FinishingRecall -= EventHandler.OnRecalling;
            Exiled.Events.Handlers.Player.ReservedSlot -= EventHandler.OnReservedSlotCheck;
            Exiled.Events.Handlers.Player.PreAuthenticating -= EventHandler.OnPreauth;
            Exiled.Events.Handlers.Server.ReportingCheater -= EventHandler.OnReportingCheater;
            Exiled.Events.Handlers.Server.LocalReporting -= EventHandler.OnReporting;
            Exiled.Events.Handlers.Server.ReloadedTranslations -= this.OnReloadedTranslations;

            EventHandler.Reset();
            Hats.Hats.Reset();
            Integrations.ClearReflection();

            UnbanPatch.LastId = null;

            ServerID = null;
            Secret = null;
            Singleton = null;

            base.OnDisabled();
        }

        private void OnReloadedTranslations()
        {
            this.OnUnregisteringCommands();
            this.OnRegisteringCommands();
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