using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Events.EventArgs;

namespace SCPStats
{
    internal static class EventHandler
    {
        private static readonly HttpClient Client = new HttpClient();

        private static string DictToString(Dictionary<string, string> dict)
        {
            var output = "{";

            foreach (var kv in dict)
            {
                output += "\"" + kv.Key + "\": \"" + kv.Value + "\", ";
            }

            return output.Substring(0, output.Length - 2) + "}";
        }
        
        private static string HmacSha256Digest(string secret, string message)
        {
            var encoding = new ASCIIEncoding();
            
            return BitConverter.ToString(new HMACSHA256(encoding.GetBytes(secret)).ComputeHash(encoding.GetBytes(message))).Replace("-", "").ToLower();
        }

        private static string HandleId(string id)
        {
            return id.Split('@')[0];
        }

        private static async Task SendRequest(Dictionary<string, string> data, string url)
        {
            var str = DictToString(data);
            
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Headers.Add("Signature", HmacSha256Digest(SCPStats.Singleton.Config.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                try
                {
                    var res = await Client.SendAsync(requestMessage);
                    res.EnsureSuccessStatusCode();

                    var body = await res.Content.ReadAsStringAsync();
#if DEBUG
                    Log.Info(body);
#endif
                    if (body == "E")
                    {
                        Log.Warn("Failed to send an event to SCPStats. Make sure that your Server ID and Secret are correct.");
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    Log.Warn(e);
#endif
                }
            }
        }

        internal static void OnRoundStart()
        {
            var data = new Dictionary<string, string>()
            {
                {"serverid", SCPStats.Singleton.Config.ServerId}
            };
            
            SendRequest(data, "https://scpstats.com/plugin/event/roundstart");
        }
        
        internal static void OnRoundEnd(RoundEndedEventArgs ev)
        {
            var data = new Dictionary<string, string>()
            {
                {"serverid", SCPStats.Singleton.Config.ServerId}
            };
            
            SendRequest(data, "https://scpstats.com/plugin/event/roundend");
        }
        
        internal static void OnKill(DiedEventArgs ev)
        {
            var data = new Dictionary<string, string>()
            {
                {"serverid", SCPStats.Singleton.Config.ServerId},
                {"playerid", HandleId(ev.Target.RawUserId)},
                {"killerrole", ((int) ev.Killer.Role).ToString()},
                {"playerrole", ((int) ev.Target.Role).ToString()},
                {"damagetype", DamageTypes.ToIndex(ev.HitInformations.GetDamageType()).ToString()}
            };
            
            SendRequest(data, "https://scpstats.com/plugin/event/death");
        }
    }
}