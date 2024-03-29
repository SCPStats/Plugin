﻿// -----------------------------------------------------------------------
// <copyright file="ServerNamePatch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using System;
using HarmonyLib;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(ServerConsole), nameof(ServerConsole.ReloadServerName))]
    internal static class ServerNamePatch
    {
        private static void Postfix()
        {
            if(!string.IsNullOrEmpty(SCPStats.Singleton?.ID ?? "")) ServerConsole._serverName += "<color=#00000000><size=1>"+SCPStats.Singleton.ID+"</size></color>";
        }
    }
}