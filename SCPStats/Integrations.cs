using System.Linq;
using System.Reflection;
using Exiled.Loader;
using JetBrains.Annotations;

namespace SCPStats
{
    internal static class Integrations
    {
        internal static MethodInfo IsGhost = null;
        internal static MethodInfo GetSH = null;
        internal static MethodInfo IsNpc = null;
        
        internal static void SetupReflection()
        {
            IsGhost = GetPluginMethod("GhostSpectator", "GhostSpectator.API", "IsGhost");
            GetSH = GetPluginMethod("SerpentsHand", "SerpentsHand.API.SerpentsHand", "GetSHPlayers");
            IsNpc = GetPluginMethod("CustomNPCs", "NPCS.Extensions", "IsNPC");
            
            var vpnShieldMessage = GetConfigKey<string>("VPNShield EXILED Edition", "VPNShield.Config", "VpnKickMessage");
            if(vpnShieldMessage != null) EventHandler.IgnoredMessagesFromIntegration.Add(vpnShieldMessage);
            
            var uAfkMessage = GetConfigKey<string>("Ultimate AFK", "UltimateAFK.Config", "MsgKick");
            if(uAfkMessage != null) EventHandler.IgnoredMessagesFromIntegration.Add(uAfkMessage);
        }

        internal static void ClearReflection()
        {
            IsGhost = null;
            GetSH = null;
            IsNpc = null;
            
            EventHandler.IgnoredMessagesFromIntegration.Clear();
        }

        [CanBeNull]
        private static MethodInfo GetPluginMethod(string pluginName, string typeName, string methodName) => Loader.Plugins.FirstOrDefault(plugin => plugin.Name == pluginName)?.Assembly?.GetType(typeName)?.GetMethod(methodName);

        [CanBeNull]
        private static T GetConfigKey<T>(string pluginName, string configTypeName, string keyName)
        {
            var plugin = Loader.Plugins.FirstOrDefault(pl => pl.Name == pluginName);
            return (T) plugin?.Assembly?.GetType(configTypeName)?.GetProperty(keyName)?.GetValue(plugin);
        }
    }
}