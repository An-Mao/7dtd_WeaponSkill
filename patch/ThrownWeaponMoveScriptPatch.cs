using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NWS_WeaponSkill.patch {
    [HarmonyPatch(typeof(ThrownWeaponMoveScript))]
    public class ThrownWeaponMoveScriptPatch {
        [HarmonyPatch("FixedUpdate")]
        [HarmonyPrefix]
        public static bool FixedUpdatePre(ThrownWeaponMoveScript __instance) {
            Log.Out($"update{__instance.timeShotStarted}");
            
            return true;
        }
        [HarmonyPatch("OnDestroy")]
        [HarmonyPrefix]
        public static bool OnDestroyPre(ThrownWeaponMoveScript __instance) {
            Log.Out("OnDestroy");
            if (GameManager.Instance == null || GameManager.Instance.World == null || __instance.firingEntity == null || __instance.transform == null || __instance.itemValueWeapon == null) {
                return true;
            }

            Log.Out("OnDestroy 1 ");
            if (__instance.stuckInEntity != null) {

                Log.Out("OnDestroy 2");
                NavObjectManager.Instance.UnRegisterNavObject(__instance.NavObject);
                if (__instance.ProjectileID != -1 && __instance.firingEntity != null && !__instance.firingEntity.isEntityRemote) {

                    Log.Out("OnDestroy 3");
                    Vector3 bellyPosition = __instance.stuckInEntity.getBellyPosition();
                    if (GameManager.Instance.World.IsChunkAreaLoaded(Mathf.CeilToInt(bellyPosition.x), Mathf.CeilToInt(bellyPosition.y), Mathf.CeilToInt(bellyPosition.z))) {

                        Log.Out("OnDestroy 4");
                        GameManager.Instance.ItemDropServer(new ItemStack(__instance.itemValueWeapon, 1), bellyPosition, Vector3.zero, __instance.ProjectileOwnerID, 1000f);
                    }
                }
            } else if (__instance.ProjectileID != -1 && __instance.firingEntity != null && !__instance.firingEntity.isEntityRemote && GameManager.Instance.World.IsChunkAreaLoaded(Mathf.CeilToInt(__instance.transform.position.x + Origin.position.x), Mathf.CeilToInt(__instance.transform.position.y + Origin.position.y), Mathf.CeilToInt(__instance.transform.position.z + Origin.position.z))) {

                Log.Out("OnDestroy 5");
                GameManager.Instance.ItemDropServer(new ItemStack(__instance.itemValueWeapon, 1), __instance.transform.position + Origin.position + Vector3.up, Vector3.zero, __instance.ProjectileOwnerID, 1000f);
            }
            return true;
        }
    }
}
