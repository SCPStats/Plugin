// -----------------------------------------------------------------------
// <copyright file="AutoUpdater.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Loader;

namespace SCPStats
{
    internal static class AutoUpdater
    {
        internal const string Version = "1.5.5-1";

        internal static async Task RunUpdater(int waitTime = 0)
        {
            if (waitTime != 0) await Task.Delay(waitTime);

            try
            {
                using (var client = new WebClient())
                {
                    if (SCPStats.Singleton == null) return;

                    var latestVersion =
                        await client.DownloadStringTaskAsync("https://scpstats.com/update/version/" +
                                                             Loader.Version.ToString());
                    if (latestVersion == "-1" || latestVersion == Version) return;

                    var location = SCPStats.Singleton?.GetPath();
                    if (location == null)
                    {
                        Log.Warn(
                            "SCPStats auto updater couldn't determine the plugin path. Make sure your plugin dll is named \"SCPStats.dll\".");
                        return;
                    }

                    var data = await client.DownloadDataTaskAsync(
                        "https://github.com/SCPStats/Plugin/releases/download/" +
                        latestVersion.Split('-')[0].Replace("/", "") + "/SCPStats.dll");

                    //This data is expected to be > 50,000 bytes, so we should only save if it is.
                    if (data.Length > 50000)
                    {
                        using (var fs = new FileStream(location, FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(data, 0, data.Length);
                        }
                        
                        Log.Info("Updated SCPStats. Please restart your server to complete the update.");
                    }
                    else
                    {
                        Log.Warn("Received invalid data while trying to download an update.");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}