// -----------------------------------------------------------------------
// <copyright file="PlayerInfo.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

namespace SCPStats
{
    internal struct PlayerInfo
    {
        internal string PlayerID;
        internal RoleType PlayerRole;
        internal bool IsAllowed;

        public PlayerInfo(string playerID, RoleType playerRole, bool isAllowed)
        {
            PlayerID = playerID;
            PlayerRole = playerRole;
            IsAllowed = isAllowed;
        }
    }
}