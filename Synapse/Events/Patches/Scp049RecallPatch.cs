﻿using System;
using Harmony;
using Mirror;
using PlayableScps;
using UnityEngine;

namespace Synapse.Events.Patches
{
    [HarmonyPatch(typeof(Scp049), nameof(Scp049.BodyCmd_ByteAndGameObject))]
    public class Scp049RecallPatch
    {
		public static bool Prefix(Scp049 __instance, ref byte num, ref GameObject go)
		{
			try
			{
				if (num == 0)
				{
					if (!__instance._interactRateLimit.CanExecute(true))
					{
						return false;
					}
					if (go == null)
					{
						return false;
					}
					if (Vector3.Distance(go.transform.position, __instance.Hub.playerMovementSync.RealModelPosition) >= Scp049.AttackDistance * 1.25f)
					{
						return false;
					}
					__instance.Hub.playerStats.HurtPlayer(new PlayerStats.HitInfo(4949f, __instance.Hub.nicknameSync.MyNick + " (" + __instance.Hub.characterClassManager.UserId + ")", DamageTypes.Scp049, __instance.Hub.queryProcessor.PlayerId), go);
					GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Sent 'death time' RPC", MessageImportance.LessImportant, false);
					__instance.Hub.scpsController.RpcTransmit_Byte(0);
					return false;
				}
				else
				{
					if (num != 1)
					{
						if (num == 2)
						{
							if (!__instance._interactRateLimit.CanExecute(true))
							{
								return false;
							}
							if (go == null)
							{
								return false;
							}
							Ragdoll component = go.GetComponent<Ragdoll>();
							if (component == null)
							{
								return false;
							}
							ReferenceHub referenceHub = null;
							foreach (GameObject player in PlayerManager.players)
							{
								ReferenceHub hub = ReferenceHub.GetHub(player);
								if (hub.queryProcessor.PlayerId == component.owner.PlayerId)
								{
									referenceHub = hub;
									break;
								}
							}
							if (referenceHub == null)
							{
								GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Request 'finish recalling' rejected; no target found", MessageImportance.LessImportant, false);
								return false;
							}
							if (!__instance._recallInProgressServer || referenceHub.gameObject != __instance._recallObjectServer || __instance._recallProgressServer < 0.85f)
							{
								GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Request 'finish recalling' rejected; Debug code: ", MessageImportance.LessImportant, false);
								GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | CONDITION#1 " + (__instance._recallInProgressServer ? "<color=green>PASSED</color>" : ("<color=red>ERROR</color> - " + __instance._recallInProgressServer.ToString())), MessageImportance.LessImportant, true);
								GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | CONDITION#2 " + ((referenceHub == __instance._recallObjectServer) ? "<color=green>PASSED</color>" : string.Concat(new object[]
								{
						"<color=red>ERROR</color> - ",
						referenceHub.queryProcessor.PlayerId,
						"-",
						(__instance._recallObjectServer == null) ? "null" : ReferenceHub.GetHub(__instance._recallObjectServer).queryProcessor.PlayerId.ToString()
								})), MessageImportance.LessImportant, false);
								GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | CONDITION#3 " + ((__instance._recallProgressServer >= 0.85f) ? "<color=green>PASSED</color>" : ("<color=red>ERROR</color> - " + __instance._recallProgressServer)), MessageImportance.LessImportant, true);
								return false;
							}
							if (referenceHub.characterClassManager.CurClass != RoleType.Spectator)
							{
								return false;
							}

							//Event
							bool allow = true;
							RoleType role = RoleType.Scp0492;
							float live = (float)referenceHub.characterClassManager.Classes.Get(RoleType.Scp0492).maxHP;
							Events.InvokeScp049RecallEvent(__instance.Hub, ref component, ref referenceHub, ref allow, ref role, ref live);

							GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Request 'finish recalling' accepted", MessageImportance.LessImportant, false);
							RoundSummary.changed_into_zombies++;
							referenceHub.characterClassManager.SetClassID(role);
							referenceHub.GetComponent<PlayerStats>().Health = live;
							if (component.CompareTag("Ragdoll"))
							{
								NetworkServer.Destroy(component.gameObject);
							}
							__instance._recallInProgressServer = false;
							__instance._recallObjectServer = null;
							__instance._recallProgressServer = 0f;
						}
						return false;
					}
					if (!__instance._interactRateLimit.CanExecute(true))
					{
						return false;
					}
					if (go == null)
					{
						return false;
					}
					Ragdoll component2 = go.GetComponent<Ragdoll>();
					if (component2 == null)
					{
						GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Request 'start recalling' rejected; provided object is not a dead body", MessageImportance.LessImportant, false);
						return false;
					}
					if (!component2.allowRecall)
					{
						GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Request 'start recalling' rejected; provided object can't be recalled", MessageImportance.LessImportant, false);
						return false;
					}
					ReferenceHub referenceHub2 = null;
					foreach (GameObject player2 in PlayerManager.players)
					{
						ReferenceHub hub2 = ReferenceHub.GetHub(player2);
						if (hub2 != null && hub2.queryProcessor.PlayerId == component2.owner.PlayerId)
						{
							referenceHub2 = hub2;
							break;
						}
					}
					if (referenceHub2 == null)
					{
						GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Request 'start recalling' rejected; target not found", MessageImportance.LessImportant, false);
						return false;
					}
					if (Vector3.Distance(component2.transform.position, __instance.Hub.PlayerCameraReference.transform.position) >= Scp049.ReviveDistance * 1.3f)
					{
						return false;
					}
					GameCore.Console.AddDebugLog("SCPCTRL", "SCP-049 | Request 'start recalling' accepted", MessageImportance.LessImportant, false);
					__instance._recallObjectServer = referenceHub2.gameObject;
					__instance._recallProgressServer = 0f;
					__instance._recallInProgressServer = true;

					return false;
				}
			}
			catch (Exception e)
			{
				Log.Error($"Scp049RecallEvent Error: {e}");
				return true;
			}
		}
	}
}
