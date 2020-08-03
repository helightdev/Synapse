﻿using System;
using System.Linq;
using Harmony;
using Synapse.Api;
using UnityEngine;

namespace Synapse.Events.Patches
{
    [HarmonyPatch(typeof(PlayerInteract), nameof(PlayerInteract.CallCmdOpenDoor))]
    public class DoorInteractPatch
    {
        public static bool Prefix(PlayerInteract __instance, GameObject doorId)
        {
            var allowTheAccess = true;
            Door door = null;
            try
            {
                if (!__instance._playerInteractRateLimit.CanExecute() ||
                    (__instance._hc.CufferId > 0 && !PlayerInteract.CanDisarmedInteract) || doorId == null ||
                    __instance._ccm.CurClass == RoleType.None || __instance._ccm.CurClass == RoleType.Spectator ||
                    !doorId.TryGetComponent(out door) || !((door.Buttons.Count == 0)
                        ? __instance.ChckDis(doorId.transform.position)
                        : door.Buttons.Any(item => __instance.ChckDis(item.button.transform.position)))) return false;
                
                __instance.OnInteract();
                
                if (__instance._sr.BypassMode) allowTheAccess = true;
                else if (door.PermissionLevels.HasPermission(Door.AccessRequirements.Checkpoints) &&
                         __instance._ccm.CurRole.team == Team.SCP) allowTheAccess = true;
                else
                {
                    try
                    {
                        if (door.PermissionLevels == 0)
                        {
                            allowTheAccess = !door.locked;
                        } 
                        else if (!door.RequireAllPermissions)
                        {
                            var itemPerms = __instance._inv.GetItemByID(__instance._inv.curItem).permissions;
                            allowTheAccess = itemPerms.Any(p =>
                                Door.backwardsCompatPermissions.TryGetValue(p, out var flag) &&
                                door.PermissionLevels.HasPermission(flag));
                        }
                        else allowTheAccess = false;
                    }
                    catch
                    {
                        allowTheAccess = false;
                    }
                }
                
                Events.InvokeDoorInteraction(__instance.gameObject.GetPlayer(), door, ref allowTheAccess);
                
                if(allowTheAccess) door.ChangeState(__instance._sr.BypassMode);
                else __instance.RpcDenied(doorId);

                return false;
            }
            catch (Exception e)
            {
                Log.Error($"DoorInteraction Error: {e}");
                
                if(allowTheAccess && door != null)
                    door.ChangeState(__instance._sr.BypassMode);
                else __instance.RpcDenied(doorId);
                return false;
            }
        }
    }
}