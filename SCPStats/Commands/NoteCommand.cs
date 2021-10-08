// -----------------------------------------------------------------------
// <copyright file="NoteCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class NoteCommand : ICommand, IUsageProvider
    {
        public string Command => SCPStats.Singleton?.Translation?.NoteCommand ?? "note";
        public string[] Aliases { get; } = SCPStats.Singleton?.Translation?.NoteCommandAliases?.ToArray() ?? Array.Empty<string>();
        public string Description => SCPStats.Singleton?.Translation?.NoteDescription ?? "Create a note about a player.";
        public string[] Usage => SCPStats.Singleton?.Translation?.NoteUsagesList ?? new string[] {"id", "message"};

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            return WarnCommand.ExecuteCustomDisplay(arguments, sender, out response, false, SCPStats.Singleton?.Translation?.NoteUsage ?? "Usage: note <id> [message]", SCPStats.Singleton?.Translation?.NoteSuccess ?? "Added note.", 5);
        }
    }
}