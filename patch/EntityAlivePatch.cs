using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace NWS_WeaponSkill.patch {
    [HarmonyPatch(typeof(EntityAlive))]
    class EntityAlivePatch {
        
        [HarmonyPatch("ProcessDamageResponseLocal")]
        [HarmonyPrefix]
        /*
        [HarmonyPatch(new Type[]{
                                typeof(DamageSource),
                                typeof(int),
                                typeof(bool),
                                typeof(Vector3),
                                typeof(float)
                                })]
        */
        public static bool ProcessDamageResponseLocalPrefix(EntityAlive __instance,ref DamageResponse _dmResponse) {
            if (__instance != null && _dmResponse.Source != null) {
                ItemClass item = _dmResponse.Source.ItemClass;
                if (item != null && item.HasAnyTags(WeaponTags.plusTags)) {
                    /*
                    item.Properties.GetFloat()
                    double addDamage = 0.01 + (__instance.GetMaxHealth() % 10) / 100;
                    addDamage = addDamage > 0.1 ? 0.1 : addDamage;
                    */
                    _dmResponse.Strength += (int)(__instance.GetMaxHealth() * item.Properties.GetFloat("DamageFix"));
                    /*
                    var package = NetPackageManager.GetPackage<NetPackageParticleEffect>().Setup(
                        new ParticleEffect("p_sparks_fuse", __instance.position,new Quaternion() ,15.0f, Color.blue),
                        _dmResponse.Source.ownerEntityId
                        );
                    ConnectionManager.Instance.SendPackage(package);
                    */
                }
            }
            return true;
        }
    }
}
