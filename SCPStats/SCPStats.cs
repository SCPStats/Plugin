using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using MEC;

namespace SCPStats
{
    public class SCPStats : Plugin<Config>
    {
        public override string Name { get; } = "ScpStats";
        public override string Author { get; } = "PintTheDragon";
        public override Version Version { get; } = new Version(1, 2, 2);
        public override Version RequiredExiledVersion { get; } = new Version(2, 2, 2);
        public override PluginPriority Priority { get; } = PluginPriority.Last;

        internal static SCPStats Singleton;

        internal string ID = "";

        private static Harmony harmony;

        private CoroutineHandle update;
        private CoroutineHandle requests;

        public override void OnEnabled()
        {
            Singleton = this;

            if (Config.Secret == "fill this" || Config.ServerId == "fill this")
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

            base.OnEnabled();
        }

        private IEnumerator<float> EnableEvents()
        {
            yield return Timing.WaitForSeconds(1f);

            if (Singleton == null) yield break;
            
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
            Exiled.Events.Handlers.Scp914.UpgradingItems += EventHandler.OnUpgrade;
            Exiled.Events.Handlers.Player.EnteringPocketDimension += EventHandler.OnEnterPocketDimension;
            Exiled.Events.Handlers.Player.Banned += EventHandler.OnBan;
            Exiled.Events.Handlers.Player.Kicked += EventHandler.OnKick;
            Exiled.Events.Handlers.Player.ChangingMuteStatus += EventHandler.OnMute;
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
            Exiled.Events.Handlers.Scp914.UpgradingItems -= EventHandler.OnUpgrade;
            Exiled.Events.Handlers.Player.EnteringPocketDimension -= EventHandler.OnEnterPocketDimension;
            Exiled.Events.Handlers.Player.Banned -= EventHandler.OnBan;
            Exiled.Events.Handlers.Player.Kicked -= EventHandler.OnKick;
            Exiled.Events.Handlers.Player.ChangingMuteStatus -= EventHandler.OnMute;

            EventHandler.Reset();
            Hats.Hats.Reset();

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