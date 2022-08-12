// -----------------------------------------------------------------------
// <copyright file="LinkSCPStatsCommand.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using RoundRestarting;

namespace SCPStats.Commands
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class LinkSCPStatsCommand : ICommand
    {
        public string Command => "linkscpstats";
        public string[] Aliases { get; } = new string[] {};
        public string Description => "Connects this server to an SCPStats server. Visit https://panel.scpstats.com/myservers for more information.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender)
            {
                response = "You do not have permission to run this command!";
                return false;
            }

            if (arguments.Array == null || arguments.Array.Length < 2)
            {
                response = "Make sure you've copied this command exactly from https://panel.scpstats.com/myservers (View Secret button).";
                return false;
            }

            var parts = arguments.Array[1].Trim().ToLower().Split(':');
            if (parts.Length != 2 || parts[0].Length != 18 || parts[1].Length != 32)
            {
                response = "Make sure you've copied this command exactly from https://panel.scpstats.com/myservers (View Secret button).";
                return false;
            }

            var id = parts[0];
            var secret = parts[1];

            var path = Path.Combine(Paths.Configs, "SCPStats");
            var serverIdPath = Path.Combine(path, Server.Port + "-ServerID.txt");
            var secretPath = Path.Combine(path, Server.Port + "-Secret.txt");

            File.WriteAllText(serverIdPath, id);
            File.WriteAllText(secretPath, secret);

            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
            RoundRestart.ChangeLevel(true);

            response = "Successfully linked server, restarting!";
            return true;
        }
    }
}