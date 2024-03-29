﻿// -----------------------------------------------------------------------
// <copyright file="RequestType.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

namespace SCPStats
{
    internal enum RequestType
    {
        RoundStart = 0,
        RoundEnd = 1,
        Spawn = 4,
        Pickup = 5,
        Drop = 6,
        Escape = 7,
        Join = 8,
        Leave = 9,
        Use = 10,
        UserInfo = 11,
        AddWarning = 15,
        GetWarnings = 16,
        DeleteWarnings = 17,
        Win = 18,
        Lose = 19,
        InvalidateBan = 20,
        KillDeath = 21,
        Revive = 22,
        PocketEnter = 23,
        PocketExit = 24,
        RoundEndPlayer = 25,
        GetAllBans = 26,
        SetVersion = 27
    }
}