// -----------------------------------------------------------------------
// <copyright file="AutoUpdater.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Loader;

namespace SCPStats
{
    internal static class AutoUpdater
    {
        private const string Version = "1.5.4-0";

        internal static async Task RunUpdater(int waitTime = 0)
        {
            if (waitTime != 0) await Task.Delay(waitTime);
            
            using (var client = new WebClient())
            {
                if (SCPStats.Singleton == null) return;

                var latestVersion = await client.DownloadStringTaskAsync("https://scpstats.com/update/version/" + Loader.Version.ToString());
                if (latestVersion == "-1" || latestVersion == Version) return;

                var location = SCPStats.Singleton?.GetPath();
                if (location == null)
                {
                    Log.Warn("SCPStats auto updater couldn't determine the plugin path. Make sure your plugin dll is named \"SCPStats.dll\".");
                    return;
                }

                await client.DownloadFileTaskAsync("https://github.com/SCPStats/Plugin/releases/download/"+latestVersion.Split('-')[0].Replace("/", "")+"/SCPStats.dll", location);
                Log.Info("Updated SCPStats. Please restart your server to complete the update.");
            }
        }
    }
}