using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ETAPI.Features;

namespace SCPStats
{
    internal static class Verification
    {
        private static readonly HttpClient client = new HttpClient();
        
        internal static async Task UpdateID()
        {
            await Task.Delay(1000);

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://scpstats.com/getid"))
            {
                var str = "{\"ip\": \"" + Server.IP + "\",\"port\": \"" + Server.Port + "\",\"id\": \"" + SCPStats.Singleton.Config.ServerId + "\"}";
                
                requestMessage.Headers.Add("Signature", Helper.HmacSha256Digest(SCPStats.Singleton.Config.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                try
                {
                    var res = await client.SendAsync(requestMessage);
                    res.EnsureSuccessStatusCode();

                    var body = await res.Content.ReadAsStringAsync();

                    if (body != "E")
                    {
                        SCPStats.Singleton.ID = body;
                        Server.AddToName(SCPStats.Singleton.ID);
                        Verify();
                        Clear();
                    }
                    else
                    {
                        Log.Warn("Error getting verification token for SCPStats. If your server is not verified, ignore this message!");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private static async Task Verify()
        {
            await Task.Delay(130000);
            
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://scpstats.com/verify"))
            {
                var str = "{\"ip\": \"" + Server.IP + "\",\"port\": \"" + Server.Port + "\",\"id\": \"" + SCPStats.Singleton.Config.ServerId + "\"}";
                
                requestMessage.Headers.Add("Signature", Helper.HmacSha256Digest(SCPStats.Singleton.Config.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                try
                {
                    var res = await client.SendAsync(requestMessage);
                    res.EnsureSuccessStatusCode();
                    
                    var body = await res.Content.ReadAsStringAsync();
                    if (body == "E")
                    {
                        Log.Warn("SCPStats Verification failed!");
                    }

                    Server.RemoveFromName(SCPStats.Singleton.ID);
                    SCPStats.Singleton.ID = "";
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Server.RemoveFromName(SCPStats.Singleton.ID);
                    SCPStats.Singleton.ID = "";
                }
            }
        }

        private static async Task Clear()
        {
            await Task.Delay(170000);

            Server.RemoveFromName(SCPStats.Singleton.ID);
            SCPStats.Singleton.ID = "";
        }
    }
}