// -----------------------------------------------------------------------
// <copyright file="Verification.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PluginAPI.Core;

namespace SCPStats
{
    internal static class Verification
    {
        private static readonly HttpClient client = new HttpClient();
        
        internal static async Task UpdateID()
        {
            await Task.Delay(1000);

            if (SCPStats.Singleton == null) return;

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://scpstats.com/getid"))
            {
                var str = "{\"ip\": \"" + ServerConsole.Ip + "\",\"port\": \"" + Server.Port + "\",\"id\": \"" + SCPStats.Singleton.ServerID + "\"}";
                
                requestMessage.Headers.Add("Signature", Helper.HmacSha256Digest(SCPStats.Singleton.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                try
                {
                    var res = await client.SendAsync(requestMessage);
                    res.EnsureSuccessStatusCode();

                    var body = await res.Content.ReadAsStringAsync();

                    if (body != "E")
                    {
                        SCPStats.Singleton.ID = body;
                        ServerConsole.ReloadServerName();
                        Verify();
                        Clear();
                    }
                    else
                    {
                        Log.Warning("Error getting verification token for SCPStats. If your server is not verified, ignore this message!");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        }

        private static async Task Verify()
        {
            await Task.Delay(130000);
            
            if (SCPStats.Singleton == null) return;
            
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://scpstats.com/verify"))
            {
                var str = "{\"ip\": \"" + ServerConsole.Ip + "\",\"port\": \"" + Server.Port + "\",\"id\": \"" + SCPStats.Singleton.ServerID + "\"}";
                
                requestMessage.Headers.Add("Signature", Helper.HmacSha256Digest(SCPStats.Singleton.Secret, str));
                requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/json");
                try
                {
                    var res = await client.SendAsync(requestMessage);
                    res.EnsureSuccessStatusCode();
                    
                    var body = await res.Content.ReadAsStringAsync();
                    if (body == "E")
                    {
                        Log.Warning("SCPStats Verification failed (ignore this unless something's actually broken)!");
                    }

                    SCPStats.Singleton.ID = "";
                    ServerConsole.ReloadServerName();
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    SCPStats.Singleton.ID = "";
                    ServerConsole.ReloadServerName();
                }
            }
        }

        private static async Task Clear()
        {
            await Task.Delay(170000);

            if (SCPStats.Singleton != null) SCPStats.Singleton.ID = "";
            ServerConsole.ReloadServerName();
        }
    }
}