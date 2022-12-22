// -----------------------------------------------------------------------
// <copyright file="Banned.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

using PluginAPI.Core;

namespace SCPStats.Exiled
{
    public class Banned
    {
        public static Player GetBanningPlayer(string identifier) => identifier.Contains("(") ? Player.Get(identifier.Substring(identifier.LastIndexOf('(') + 1).TrimEnd(')')) : default;
    }
}