// -----------------------------------------------------------------------
// <copyright file="DeleteWarningCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using NWAPIPermissionSystem;
using PluginAPI.Core;
using RemoteAdmin;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DeleteWarningCommand : ICommand, IUsageProvider
    {
        public string Command => SCPStats.Singleton?.Translation?.DeleteWarningCommand ?? "deletewarning";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.DeleteWarningCommandAliases?.ToArray() ?? new string[] {"deletewarnings", "delwarning", "delwarnings", "delwarn", "deletewarns", "deletewarn", "delwarns"};
        public string Description => SCPStats.Singleton?.Translation?.DeleteWarningDescription ?? "Delete a warning.";
        public string[] Usage => SCPStats.Singleton?.Translation?.DeleteWarningUsages ?? new string[] {"id"};

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player p = null;
            if (sender is PlayerCommandSender commandSender)
            {
                p = Player.Get(commandSender.ReferenceHub);
                if (!p.CheckPermission("scpstats.deletewarning"))
                {
                    response = SCPStats.Singleton?.Translation?.NoPermissionMessage ?? "You do not have permission to run this command!";
                    return false;
                }
            }

            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = SCPStats.Singleton?.Translation?.DeleteWarningUsage ?? "Usage: deletewarning <id>";
                return false;
            }

            if (!int.TryParse(arguments.Array[1], out var id))
            {
                response = SCPStats.Singleton?.Translation?.DeleteWarningIdNotNumeric ?? "Warning IDs cannot contain non-numbers!";
                return false;
            }

            Helper.HandleBooleanTask(p, SCPStats.Singleton?.Translation?.WarningDeleted ?? "Successfully deleted warning!", SCPStats.Singleton?.Translation?.DeleteWarningCommand?.ToUpper() ?? "DELETEWARNING", API.API.DeleteWarning(id, Helper.HandleId(p) ?? ""));

            response = SCPStats.Singleton?.Translation?.PleaseWait ?? "Please wait...";
            return true;
        }
    }
}