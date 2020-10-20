using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace SCPStats
{
    public class EventHandler
    {
        private static readonly HttpClient Client = new HttpClient();

        private static string DictToString(Dictionary<string, string> dict)
        {
            var output = "{";

            foreach (var kv in dict)
            {
                output += "\"" + kv.Key + "\": \"" + kv.Value + "\", ";
            }

            return output.Substring(0, output.Length - 1) + "}";
        }
        
        public static string HmacSha256Digest(string secret, string message)
        {
            var encoding = new ASCIIEncoding();
            var keyBytes = encoding.GetBytes(secret);
            var messageBytes = encoding.GetBytes(message);
            var cryptographer = new HMACSHA256(keyBytes);

            var bytes = cryptographer.ComputeHash(messageBytes);

            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        private static void SendRequest(Dictionary<string, string> data, string url)
        {
            var str = DictToString(data);
            
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://your.site.com"))
            {
                requestMessage.Headers.Add("Signature", HmacSha256Digest(SCPStats.Singleton.Config.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                Client.SendAsync(requestMessage);
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
    }
}