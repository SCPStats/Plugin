using System.IO;
using System.Net;
using System.Threading.Tasks;
using Exiled.API.Features;

namespace SCPStats
{
    internal static class AutoUpdater
    {
        private static string Version = "1.1.6-1";

        internal static async Task RunUpdater(int waitTime = 0)
        {
            if (waitTime != 0) await Task.Delay(waitTime);
            
            using (var client = new WebClient())
            {
                var res = await client.DownloadStringTaskAsync("https://scpstats.com/update/version");
                if (res == Version) return;
                await client.DownloadFileTaskAsync("https://scpstats.com/update/SCPStats.dll", Path.Combine(Paths.Plugins, Path.GetFileName(SCPStats.Singleton.Assembly.Location)));
                Log.Info("Updated SCPStats. Please restart your server to complete the update.");
            }
        }
    }
}