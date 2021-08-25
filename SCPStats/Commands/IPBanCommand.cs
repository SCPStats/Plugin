using System;
using System.Linq;
using System.Text.RegularExpressions;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;
using SCPStats.Websocket;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class IPBanCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.IpBanCommand ?? "ipban";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.IpBanCommandAliases?.ToArray() ?? new string[] { "banip" };
        public string Description => SCPStats.Singleton?.Translation?.IpBanDescription ?? "Ban an IP.";

        private static Regex _ipRegex = new Regex("((^\\s*((([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]))\\s*$)|(^\\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:)))(%.+)?\\s*$))", RegexOptions.Multiline);

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var issuerID = "";
            var issuerName = "";

            if (sender is PlayerCommandSender commandSender)
            {
                var p = Player.Get(commandSender.ReferenceHub);

                issuerID = Helper.HandleId(p);
                issuerName = p.Nickname;
            }

            if (arguments.Array == null || arguments.Array.Length < 3)
            {
                response = SCPStats.Singleton?.Translation?.IpBanUsage ?? "Usage: ipban <id> <duration> [reason]";
                return false;
            }

            var ip = arguments.Array[1].Trim().ToLower();
            var ipPlayer = Player.Get(ip);
            var isIp = _ipRegex.IsMatch(ip);

            //If it is a player
            if (ipPlayer != null && !isIp)
            {
                ip = ipPlayer.IPAddress;
            }
            //If it isn't an ip (and not a player)
            else if (!isIp)
            {
                response = SCPStats.Singleton?.Translation?.IpBanInvalidIp ?? "Please enter a valid IP (for example, 1.1.1.1)!";
                return false;
            }
            //If it is an ip
            else
            {
                if (!sender.CheckPermission("scpstats.ipban"))
                {
                    response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to run this command!";
                    return false;
                }
            }

            var duration = 0L;
            try
            {
                duration = Misc.RelativeTimeToSeconds(arguments.Array[2], 60);
            }
            catch(Exception e)
            {
                response = e.Message;
                return false;
            }

            if (duration < 0L)
            {
                duration = 0L;
            }
            if (duration == 0L && !sender.CheckPermission(new PlayerPermissions[]
            {
                PlayerPermissions.KickingAndShortTermBanning,
                PlayerPermissions.BanningUpToDay,
                PlayerPermissions.LongTermBanning
            }, out response)) 
            {
                return false;
            }
            if (duration > 0L && duration <= 3600L && !sender.CheckPermission(PlayerPermissions.KickingAndShortTermBanning, out response))
            {
                return false;
            }
            if (duration > 3600L && duration <= 86400L && !sender.CheckPermission(PlayerPermissions.BanningUpToDay, out response))
            {
                return false;
            }
            if (duration > 86400L && !sender.CheckPermission(PlayerPermissions.LongTermBanning, out response))
            {
                return false;
            }

            var message = "";
            if (arguments.Array.Length > 3)
            {
                message = string.Join(" ", arguments.Array.Skip(3));
            }

            CommandSender commandSender1;
            var allowCheck = (commandSender1 = (sender as CommandSender)) != null && !commandSender1.FullPermissions;

            foreach (var player in Player.List)
            {
                if (player.IPAddress.Trim().ToLower() != ip) continue;

                if (player.IsStaffBypassEnabled)
                {
                    response = SCPStats.Singleton?.Translation?.IpBanCantBan ?? "You cannot ban this IP!";
                    return false;
                }

                if (!allowCheck) continue;

                var requiredKickPower = player.Group?.RequiredKickPower ?? 0;
                if (requiredKickPower <= commandSender1.KickPower) continue;

                response = SCPStats.Singleton?.Translation?.IpBanCantBan ?? "You cannot ban this IP!";
                return false;
            }

            foreach (var player in Player.List)
            {
                if (player.IPAddress.Trim().ToLower() != ip) continue;

                KickPlayer(player, duration, message);
            }

            WebsocketHandler.SendRequest(RequestType.AddWarning, "{\"type\":\"6\",\"playerId\":\""+ip+"\",\"message\":\""+message.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\",\"length\":"+duration+",\"playerName\":\"\",\"issuer\":\""+issuerID+"\",\"issuerName\":\""+issuerName.Replace("\\", "\\\\").Replace("\"", "\\\"")+"\"}");

            response = SCPStats.Singleton?.Translation?.IpBanSuccess ?? "Successfully banned IP!";
            return true;
        }

        private static void KickPlayer(Player p, long duration, string message)
        {
            ServerConsole.Disconnect(p.GameObject, (SCPStats.Singleton?.Translation?.BannedMessage ?? "[SCPStats] You have been banned from this server:\nExpires in: {duration}.\nReason: {reason}.").Replace("{duration}", Helper.SecondsToString((int) duration)).Replace("{reason}", message));
        }
    }
}