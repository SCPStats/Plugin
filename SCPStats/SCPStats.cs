using System;
using Exiled.API.Enums;
using Exiled.API.Features;

namespace SCPStats
{
    public class SCPStats : Plugin<Config>
    {
        public override string Name { get; } = "ScpStats";
        public override string Author { get; } = "PintTheDragon";
        public override Version Version { get; } = new Version(1, 1, 0);
        public override PluginPriority Priority { get; } = PluginPriority.Last;

        internal static SCPStats Singleton;

        public override void OnEnabled()
        {
            base.OnEnabled();

            Singleton = this;

            if (Config.Secret == "fill this" || Config.ServerId == "fill this")
            {
                Log.Warn("Config for SCPStats has not been filled out correctly. Disabling!");
                base.OnDisabled();
                return;
            }
            
            EventHandler.Start();

            Exiled.Events.Handlers.Server.RoundStarted += EventHandler.OnRoundStart;
            Exiled.Events.Handlers.Server.RoundEnded += EventHandler.OnRoundEnd;
            Exiled.Events.Handlers.Server.RestartingRound += EventHandler.OnRoundRestart;
            Exiled.Events.Handlers.Server.WaitingForPlayers += EventHandler.Waiting;
            Exiled.Events.Handlers.Player.Dying += EventHandler.OnKill;
            Exiled.Events.Handlers.Player.ChangingRole += EventHandler.OnRoleChanged;
            Exiled.Events.Handlers.Player.PickingUpItem += EventHandler.OnPickup;
            Exiled.Events.Handlers.Player.DroppingItem += EventHandler.OnDrop;
            Exiled.Events.Handlers.Player.Joined += EventHandler.OnJoin;
            Exiled.Events.Handlers.Player.Left += EventHandler.OnLeave;
            Exiled.Events.Handlers.Player.MedicalItemUsed += EventHandler.OnUse;
            Exiled.Events.Handlers.Player.ThrowingGrenade += EventHandler.OnThrow;
        }

        public override void OnDisabled()
        {
            Singleton = null;
            
            Exiled.Events.Handlers.Server.RoundStarted -= EventHandler.OnRoundStart;
            Exiled.Events.Handlers.Server.RoundEnded -= EventHandler.OnRoundEnd;
            Exiled.Events.Handlers.Server.RestartingRound -= EventHandler.OnRoundRestart;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= EventHandler.Waiting;
            Exiled.Events.Handlers.Player.Dying -= EventHandler.OnKill;
            Exiled.Events.Handlers.Player.PickingUpItem -= EventHandler.OnPickup;
            Exiled.Events.Handlers.Player.DroppingItem -= EventHandler.OnDrop;
            Exiled.Events.Handlers.Player.Joined -= EventHandler.OnJoin;
            Exiled.Events.Handlers.Player.Left -= EventHandler.OnLeave;
            Exiled.Events.Handlers.Player.MedicalItemUsed -= EventHandler.OnUse;
            Exiled.Events.Handlers.Player.ThrowingGrenade -= EventHandler.OnThrow;
            
            EventHandler.Reset();

            base.OnDisabled();
        }
    }
}