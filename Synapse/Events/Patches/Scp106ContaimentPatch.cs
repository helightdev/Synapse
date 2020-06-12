﻿using System;
using Harmony;

namespace Synapse.Events.Patches
{
    [HarmonyPatch(typeof(PlayerInteract), nameof(PlayerInteract.CallCmdContain106))]
    public class Scp106ContaimentPatch
    {
        public static bool Prefix(PlayerInteract __instance)
        {
            try
            {
                var allow = true;
                Events.InvokeScp106ContaimentEvent(__instance.gameObject.GetComponent<ReferenceHub>(), ref allow);

                return allow;
            }
            catch (Exception e)
            {
                Log.Error($"Scp106ContaimentErr: {e}");
                return true;
            }
        }
    }
}