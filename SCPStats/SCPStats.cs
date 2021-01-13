using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
using PluginFramework;
using UnityEngine;
using VirtualBrightPlayz.SCP_ET;
using VirtualBrightPlayz.SCP_ET.Misc;

namespace SCPStats
{
    public class SCPStats : Plugin
    {
        internal Config Config;
        
        internal static SCPStats Singleton;

        internal string ID = "";

        private static Harmony harmony;

        internal float waitTime = 10;

        private CoroutineHandle update;

        public override void OnEnable()
        {
            Singleton = this;

            Config = ETAPI.Features.Config.AddConfig<Config>("SCPStats");

            if (Config.Secret == "fill this" || Config.ServerId == "fill this")
            {
                Log.Warn("Config for SCPStats has not been filled out correctly. Disabling!");
                return;
            }
            
            harmony = new Harmony("SCPStats");
            harmony.PatchAll();
            
            EventHandler.Start();

            if (Config.AutoUpdates)
            {
                AutoUpdater.RunUpdater(10000);
                update = Timing.RunCoroutine(AutoUpdates());
            }

            Log.Info("SCPStats by PintTheDragon has been enabled!");
        }

        public override void OnDisable()
        {
            harmony.UnpatchAll();
            harmony = null;

            Timing.KillCoroutines(update);

            EventHandler.Reset();
            Hats.Hats.Reset();

            waitTime = 10f;
            
            Singleton = null;

            Log.Info("SCPStats by PintTheDragon has been disabled!");
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