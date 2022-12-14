// -----------------------------------------------------------------------
// <copyright file="PlayerInfo.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using PlayerRoles;

namespace SCPStats
{
    internal struct PlayerInfo
    {
        internal string PlayerID;
        internal RoleTypeId PlayerRole;
        internal bool IsAllowed;

        public PlayerInfo(string playerID, RoleTypeId playerRole, bool isAllowed)
        {
            PlayerID = playerID;
            PlayerRole = playerRole;
            IsAllowed = isAllowed;
        }
    }
}