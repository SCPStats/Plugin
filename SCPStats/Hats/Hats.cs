// -----------------------------------------------------------------------
// <copyright file="Hats.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Features;
using SCPStats.Commands;
using UnityEngine;

namespace SCPStats.Hats
{
    internal static class Hats
    {
        internal static void SpawnCurrentHat(this Player p)
        {
            if (!HatCommand.HatPlayers.ContainsKey(p.UserId)) return;

            p.SpawnHat(HatCommand.HatPlayers[p.UserId]);
        }
        
        internal static void SpawnHat(this Player p, HatInfo hat)
        {
            if (!SCPStats.Singleton?.Config.EnableHats ?? true) return;
            
            API.API.SpawnHat(p, hat);
        }

        internal static Vector3 GetHatPosForRole(RoleType role)
        {
            switch (role)
            {
                case RoleType.Scp173:
                    return new Vector3(0, .7f, -.05f);
                case RoleType.Scp106:
                    return new Vector3(0, .45f, .13f);
                case RoleType.Scp096:
                    return new Vector3(.15f, .45f, .225f);
                case RoleType.Scp93953:
                    return new Vector3(0, -.5f, 1.125f);
                case RoleType.Scp93989:
                    return new Vector3(0, -.3f, 1.1f);
                case RoleType.Scp049:
                    return new Vector3(0, .125f, -.05f);
                case RoleType.None:
                    return new Vector3(-1000, -1000, -1000);
                case RoleType.Spectator:
                    return new Vector3(-1000, -1000, -1000);
                case RoleType.Scp0492:
                    return new Vector3(0, 0f, -.06f);
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