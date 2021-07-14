// -----------------------------------------------------------------------
// <copyright file="SilentWarnCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SilentWarnCommand : ICommand
    {
        public string Command => SCPStats.Singleton?.Translation?.SilentWarnCommand ?? "swarn";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.SilentWarnCommandAliases?.ToArray() ?? new string[] {"silentwarn"};
        public string Description => SCPStats.Singleton?.Translation?.SilentWarnDescription ?? "Silently warn a player (without showing a message on their screen).";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            return WarnCommand.ExecuteCustomDisplay(arguments, sender, out response, false, SCPStats.Singleton?.Translation?.SilentWarnUsage ?? "Usage: swarn <id> [reason]", SCPStats.Singleton?.Translation?.SilentWarnSuccess ?? "Added warning.");
        }
    }
}