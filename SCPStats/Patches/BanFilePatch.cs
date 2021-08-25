// -----------------------------------------------------------------------
// <copyright file="BanFilePatch.cs" company="SCPStats.com">
// Copyright (c) SCPStats.com. All rights reserved.
// Licensed under the Apache v2 license.
// </copyright>
// -----------------------------------------------------------------------

using HarmonyLib;

namespace SCPStats.Patches
{
    [HarmonyPatch(typeof(FileManager), nameof(FileManager.AppendFile))]
    public class BanFilePatch
    {
        public static bool Prefix(string data, string path, bool newLine = true)
        {
            if (SCPStats.Singleton?.Config?.DisableBasegameBans ?? false) return false;
            return true;
        }
    }
}