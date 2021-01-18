using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MEC;
using Synapse.Api.Events.SynapseEventArguments;
using Synapse.Api.Plugin;

namespace SCPStats
{
    [PluginInformation(
        Author = "PintTheDragon",
        Description = "Records stats for SCPStats.com",
        Name = "SCPStats",
        SynapseMajor = 2,
        SynapseMinor = 2,
        SynapsePatch = 0,
        Version = "1.8.0",
        LoadPriority = int.MinValue
    )]
    public class SCPStats : AbstractPlugin
    {
        [Synapse.Api.Plugin.Config(section = "SCPStats")]
        public Config Config { get; set; }
        
        internal static SCPStats Singleton;

        internal string ID = "";

        private static Harmony harmony;

        internal float waitTime = 10;
        
        private CoroutineHandle update;

        public override void Load()
        {
            Singleton = this;

            if (Config.Secret == "fill this" || Config.ServerId == "fill this")
            {
                Log.Warn("Config for SCPStats has not been filled out correctly. Disabling!");
                return;
            }
            
            harmony = new Harmony("SCPStats");
            harmony.PatchAll();
            
            EventHandler.Start();

            Synapse.Api.Events.EventHandler.Get.Round.RoundStartEvent += EventHandler.OnRoundStart;
            Synapse.Api.Events.EventHandler.Get.Round.RoundEndEvent += EventHandler.OnRoundEnd;
            Synapse.Api.Events.EventHandler.Get.Round.RoundRestartEvent += EventHandler.OnRoundRestart;
            Synapse.Api.Events.EventHandler.Get.Round.WaitingForPlayersEvent += EventHandler.Waiting;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerDeathEvent += EventHandler.OnKill;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerSetClassEvent += EventHandler.OnRoleChanged;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerPickUpItemEvent += EventHandler.OnPickup;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerDropItemEvent += EventHandler.OnDrop;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerJoinEvent += EventHandler.OnJoin;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerLeaveEvent += EventHandler.OnLeave;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerItemUseEvent += EventHandler.OnUse;
            Synapse.Api.Events.EventHandler.Get.Map.Scp914ActivateEvent += EventHandler.OnUpgrade;

            if (Config.AutoUpdates)
            {
                AutoUpdater.RunUpdater(10000);
                update = Timing.RunCoroutine(AutoUpdates());
            }
            
            Log.Info("SCPStats by PintTheDragon has loaded!");
        }

        public void OnDisabled()
        {
            harmony.UnpatchAll();
            harmony = null;
            
            Synapse.Api.Events.EventHandler.Get.Round.RoundStartEvent -= EventHandler.OnRoundStart;
            Synapse.Api.Events.EventHandler.Get.Round.RoundEndEvent -= EventHandler.OnRoundEnd;
            Synapse.Api.Events.EventHandler.Get.Round.RoundRestartEvent -= EventHandler.OnRoundRestart;
            Synapse.Api.Events.EventHandler.Get.Round.WaitingForPlayersEvent -= EventHandler.Waiting;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerDeathEvent -= EventHandler.OnKill;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerSetClassEvent -= EventHandler.OnRoleChanged;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerPickUpItemEvent -= EventHandler.OnPickup;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerDropItemEvent -= EventHandler.OnDrop;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerJoinEvent -= EventHandler.OnJoin;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerLeaveEvent -= EventHandler.OnLeave;
            Synapse.Api.Events.EventHandler.Get.Player.PlayerItemUseEvent -= EventHandler.OnUse;
            Synapse.Api.Events.EventHandler.Get.Map.Scp914ActivateEvent -= EventHandler.OnUpgrade;

            EventHandler.Reset();
            Hats.Hats.Reset();

            waitTime = 10f;
            
            Singleton = null; ;
        }

        public override void ReloadConfigs()
        {
            OnDisabled();
            Load();
        }
        
        private IEnumerator<float> AutoUpdates()
        {
            yield return Timing.WaitForSeconds(7200);

            AutoUpdater.RunUpdater(10000);
        }
    }
}