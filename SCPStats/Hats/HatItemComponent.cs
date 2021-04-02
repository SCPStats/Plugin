// -----------------------------------------------------------------------
// <copyright file="HatItemComponent.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using UnityEngine;

namespace SCPStats.Hats
{
    internal class HatItemComponent : MonoBehaviour
    {
        internal HatPlayerComponent player;
        internal Vector3 pos;
        internal Vector3 itemOffset;
        internal Quaternion rot;
    }
}