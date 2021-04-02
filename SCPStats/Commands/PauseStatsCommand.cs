// -----------------------------------------------------------------------
// <copyright file="PauseStatsCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using Exiled.Permissions.Extensions;
using RemoteAdmin;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class PauseStatsCommand : ICommand
    {
        public string Command { get; } = "pausestats";
        public string[] Aliases { get; } = new string[] {"pausestat", "pausescpstats", "pausescpstat", "pauseround"};
        public string Description { get; } = "Temporarily pause stat collection for the round. Useful for events.";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool perms;

            if (sender is PlayerCommandSender p)
            {
                perms = p.CheckPermission("scpstats.pause");
            }
            else
            {
                perms = true;
            }

            if (!perms)
            {
                response = "You do not have permission to run this command! Missing permission: scpstats.pause";
                return true;
            }
            
            EventHandler.PauseRound = true;
            response = "Successfully paused stat collection for the round.";
            return true;
        }
    }
}