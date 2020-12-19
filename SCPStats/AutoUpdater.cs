using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SCPStats
{
    internal static class AutoUpdater
    {
        private const string Version = "1.1.8-2";

        internal static async Task RunUpdater(int waitTime = 0)
        {
            if (waitTime != 0) await Task.Delay(waitTime);
            
            using (var client = new WebClient())
            {
                var res = await client.DownloadStringTaskAsync("https://scpstats.com/update/version");
                if (res == Version) return;

                var files = Directory.GetFiles(SynapseController.Server.Files.SharedPluginDirectory).ToList();
                files.AddRange(Directory.GetFiles(SynapseController.Server.Files.PluginDirectory));
                
                var location = files.FirstOrDefault(path => path.ToLower().Contains("scpstats") && path.EndsWith(".dll"));
                if (location == null)
                {
                    Log.Warn("SCPStats auto updater couldn't determine the plugin path. Make sure your plugin dll is named \"SCPStats.dll\".");
                    return;
                }
                
                var githubVer = await client.DownloadStringTaskAsync("https://scpstats.com/update/github");
                await client.DownloadFileTaskAsync("https://github.com/SCPStats/Plugin/releases/download/"+githubVer.Replace("/", "")+"/SCPStats.dll", location);
                Log.Info("Updated SCPStats. Please restart your server to complete the update.");
            }
        }
    }
}