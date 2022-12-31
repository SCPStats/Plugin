// -----------------------------------------------------------------------
// <copyright file="Hats.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Features;
using PlayerRoles;
using SCPStats.Commands;
using UnityEngine;

namespace SCPStats.Hats
{
    internal static class Hats
    {
        internal static void SpawnCurrentHat(this Player p)
        {
            if (!HatCommand.HatPlayers.ContainsKey(p.UserId)) return;

            p.SpawnHat(HatCommand.HatPlayers[p.UserId].Item1, EventHandler.UserInfo.TryGetValue(Helper.HandleId(p), out var info) && info.Item2.ShowHat);
        }
        
        internal static void SpawnHat(this Player p, HatInfo hat, bool showHat)
        {
            if (!(SCPStats.Singleton?.Config?.EnableHats ?? true)) return;
            
            API.API.SpawnHat(p, hat, showHat);
        }

        internal static Vector3 GetHatPosForRole(RoleTypeId role)
        {
            switch (role)
            {
                case RoleTypeId.Scp173:
                    return new Vector3(0, .55f, -.05f);
                case RoleTypeId.Scp106:
                    return new Vector3(0, .45f, .18f);
                case RoleTypeId.Scp096:
                    return new Vector3(.15f, .425f, .325f);
                case RoleTypeId.Scp939:
                    // TODO: Fix.
                    return new Vector3(0, -.5f, 1.125f);
                case RoleTypeId.Scp049:
                    return new Vector3(0, .125f, -.05f);
                case RoleTypeId.None:
                    return new Vector3(-1000, -1000, -1000);
                case RoleTypeId.Spectator:
                    return new Vector3(-1000, -1000, -1000);
                case RoleTypeId.Scp0492:
                    return new Vector3(0, .1f, -.16f);
                default:
                    return new Vector3(0, .15f, -.07f);
            }
        }

        internal static void Reset()
        {
            foreach (var component in Object.FindObjectsOfType<HatPlayerComponent>())
            {
                if (component.item)
                {
                    Object.Destroy(component.item.gameObject);
                }

                Object.Destroy(component);
            }

            foreach (var component in Object.FindObjectsOfType<HatItemComponent>())
            {
                Object.Destroy(component.gameObject);
            }
        }
    }
}