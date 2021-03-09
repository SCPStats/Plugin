using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using SCPStats.Patches;
using SCPStats.Websocket;

namespace SCPStats
{
    public class SCPStats : Plugin<Config>
    {
        public override string Name { get; } = "SCPStats";
        public override string Author { get; } = "PintTheDragon";
        public override Version Version { get; } = new Version(1, 2, 4);
        public override Version RequiredExiledVersion { get; } = new Version(2, 8, 0);
        public override PluginPriority Priority { get; } = PluginPriority.Last;
        public override string Prefix { get; } = "scp_stats";

        internal static SCPStats Singleton;

        internal static string ServerID = "fill this";
        internal static string Secret = "fill this";

        internal string ID = "";

        private static Harmony harmony;

        private CoroutineHandle update;
        private CoroutineHandle requests;
        
        private static Regex AlphaNums = new Regex("[^A-Za-z0-9]");

        public override void OnEnabled()
        {
            Singleton = this;
            
            LoadConfigs();

            if (Secret == "fill this" || ServerID == "fill this")
            {
                Log.Warn("Config for SCPStats has not been filled out correctly. Disabling!");
                this.OnUnregisteringCommands();
                this.OnDisabled();
                return;
            }
            
            harmony = new Harmony("SCPStats-"+Version);
            harmony.PatchAll();
            
            EventHandler.Start();

            Timing.RunCoroutine(EnableEvents());

            if (Config.AutoUpdates)
            {
                AutoUpdater.RunUpdater(10000);
                update = Timing.RunCoroutine(AutoUpdates());
            }

            requests = Timing.RunCoroutine(WebsocketRequests.DequeueRequests());

            EventHandler.OnRAReload();

            base.OnEnabled();
        }

        private IEnumerator<float> EnableEvents()
        {
            yield return Timing.WaitForSeconds(1f);

            if (Singleton == null) yield break;
            
            Helper.SetupReflection();
            
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
            Exiled.Events.Handlers.Player.MedicalItemDequipped += EventHandler.OnUse;
            Exiled.Events.Handlers.Player.ThrowingGrenade += EventHandler.OnThrow;
            Exiled.Events.Handlers.Server.ReloadedRA += EventHandler.OnRAReload;
            Exiled.Events.Handlers.Server.ReloadedConfigs += LoadConfigs;
            Exiled.Events.Handlers.Scp914.UpgradingItems += EventHandler.OnUpgrade;
            Exiled.Events.Handlers.Player.EnteringPocketDimension += EventHandler.OnEnterPocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += EventHandler.OnEscapingPocketDimension;
            Exiled.Events.Handlers.Player.Banned += EventHandler.OnBan;
            Exiled.Events.Handlers.Player.Kicked += EventHandler.OnKick;
            Exiled.Events.Handlers.Player.ChangingMuteStatus += EventHandler.OnMute;
            Exiled.Events.Handlers.Player.ChangingIntercomMuteStatus += EventHandler.OnIntercomMute;
            Exiled.Events.Handlers.Scp049.FinishingRecall += EventHandler.OnRecalling;
        }

        private static void LoadConfigs()
        {
            if (Singleton == null) return;
            if (!Singleton.Config.SeparateConfig)
            {
                ServerID = Singleton.Config.ServerId;
                Secret = Singleton.Config.Secret;

                return;
            }

            if (Paths.Configs == null) return;

            var path = Path.Combine(Paths.Configs, "SCPStats");
            Directory.CreateDirectory(path);

            var serverIdPath = Path.Combine(path, Server.Port + "-ServerID.txt");
            var secretPath = Path.Combine(path, Server.Port + "-Secret.txt");

            var flag = false;
            
            if (!File.Exists(serverIdPath))
            {
                var stream = File.CreateText(serverIdPath);
                stream.Write("fill this");
                stream.Dispose();
                
                flag = true;
            }
            
            if (!File.Exists(secretPath))
            {
                var stream = File.CreateText(secretPath);
                stream.Write("fill this");
                stream.Dispose();
                
                flag = true;
            }

            if (flag) return;

            ServerID = AlphaNums.Replace(File.ReadAllText(serverIdPath), "");
            Secret = AlphaNums.Replace(File.ReadAllText(secretPath), "");
        }

        public override void OnDisabled()
        {
            harmony?.UnpatchAll();
            harmony = null;

            Timing.KillCoroutines(update, requests);

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
            Exiled.Events.Handlers.Player.MedicalItemDequipped -= EventHandler.OnUse;
            Exiled.Events.Handlers.Player.ThrowingGrenade -= EventHandler.OnThrow;
            Exiled.Events.Handlers.Server.ReloadedRA -= EventHandler.OnRAReload;
            Exiled.Events.Handlers.Server.ReloadedConfigs -= LoadConfigs;
            Exiled.Events.Handlers.Scp914.UpgradingItems -= EventHandler.OnUpgrade;
            Exiled.Events.Handlers.Player.EnteringPocketDimension -= EventHandler.OnEnterPocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= EventHandler.OnEscapingPocketDimension;
            Exiled.Events.Handlers.Player.Banned -= EventHandler.OnBan;
            Exiled.Events.Handlers.Player.Kicked -= EventHandler.OnKick;
            Exiled.Events.Handlers.Player.ChangingMuteStatus -= EventHandler.OnMute;
            Exiled.Events.Handlers.Player.ChangingIntercomMuteStatus -= EventHandler.OnIntercomMute;
            Exiled.Events.Handlers.Scp049.FinishingRecall -= EventHandler.OnRecalling;

            EventHandler.Reset();
            Hats.Hats.Reset();
            Helper.ClearReflection();

            UnbanPatch.LastId = null;

            ServerID = null;
            Secret = null;
            Singleton = null;

            base.OnDisabled();
        }

        private IEnumerator<float> AutoUpdates()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(7200);

                AutoUpdater.RunUpdater(10000);
            }
        }
    }
}