using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ItemActionThrowAway;

namespace NWS_WeaponSkill {
    public class ThrowingScript : MonoBehaviour {
        private static System.Threading.SynchronizationContext unityContext;

        void Awake() {
            unityContext = System.Threading.SynchronizationContext.Current;
            if (unityContext == null) {
                Debug.LogError("UnitySynchronizationContext not found.  Make sure this script is running in the Unity main thread.");
            }
        }

        public void HandleThrow(ItemActionThrownWeapon __instance, MyInventoryData _actionData) {
            // 获取当前状态 (例如，投掷强度，目标等)
            float throwStrength = _actionData.m_ThrowStrength;
            Vector3 playerPosition = new Vector3(); //transform.position; // 或者从 _actionData 中获取
            Vector3 lookDirection = new Vector3(); //transform.forward; // 或者从 _actionData 中获取

            // 复制数据，确保线程安全。 这是关键！
            // 避免直接传递 _actionData 或 __instance，因为它们可能在其他线程中被修改。
            // 创建一个简单的数据结构来传递所需的信息。
            var throwData = new ThrowData {
                ThrowStrength = throwStrength,
                PlayerPosition = playerPosition,
                LookDirection = lookDirection,
                ActionData = _actionData,  // 注意：如果 _actionData 包含 Unity 对象，不要直接传递！！
                Instance = __instance       // 注意：如果 __instance 包含 Unity 对象，不要直接传递！！
            };

            // 将投掷逻辑调度到主线程
            unityContext.Post(_ =>
            {
                // 在主线程上执行投掷逻辑
                PerformThrow(throwData);
            }, null);

            // 立即返回，不阻塞当前线程
            Debug.Log("Throw request submitted to main thread.");
        }

        private void PerformThrow(ThrowData throwData) {
            ItemActionThrownWeapon __instance = throwData.Instance;
            MyInventoryData _actionData = throwData.ActionData;

            // 你的投掷逻辑代码 (从你提供的代码修改而来，但使用 throwData 中的数据)
            if (_actionData != null) {
                ItemInventoryData invData = _actionData.invData;
                if (invData != null) {
                    ItemValue item = invData.itemValue;
                    if (item != null && item.ItemClass.HasAnyTags(WeaponTags.allTags)) {
                        if (invData.holdingEntity is EntityPlayerLocal player) {
                            float v = __instance.Properties.GetFloat("ThrownRadius");
                            System.Collections.Generic.List<EntityAlive> targets = new System.Collections.Generic.List<EntityAlive>();
                            FindNearbyEntities(player, v, targets);
                            v = __instance.Properties.GetFloat("ExSkillTriggerNumber");

                            if (_actionData.m_ThrowStrength == __instance.maxThrowStrength) {
                            } else {
                                if (targets.Count > v) {
                                    int c = __instance.Properties.GetInt("ExSkillNumber");
                                    int cc = 0;
                                    foreach (EntityAlive target in targets) {
                                        if (cc < c) {
                                            cc++;
                                            Vector3 targetHead = target.getHeadPosition();
                                            ThrownWeaponMoveScript thrownWeaponMoveScript = __instance.instantiateProjectile(_actionData);
                                            Vector3 origin = targetHead;
                                            Vector3 direction = (target.position - origin).normalized;
                                            thrownWeaponMoveScript.Fire(origin, direction, player, __instance.hitmaskOverride, __instance.maxThrowStrength);
                                            ((Component)invData.model).gameObject.SetActive(true);
                                            GameObject.Destroy(thrownWeaponMoveScript.gameObject, __instance.LifeTime);
                                        }
                                    }
                                } else {
                                    int c = __instance.Properties.GetInt("SkillCount");

                                    EntityAlive nearestEntity = GetNearestEntity(player, targets);
                                    for (int i = 0; i < c; i++) {
                                        ThrownWeaponMoveScript thrownWeaponMoveScript = __instance.instantiateProjectile(_actionData);
                                        Vector3 randomOffset = new Vector3(
                                            UnityEngine.Random.Range(-2f, 2f),
                                            1f,
                                            UnityEngine.Random.Range(-2f, 2f)
                                        );
                                        Vector3 origin = player.GetLookRay().origin + randomOffset;
                                        Vector3 direction = nearestEntity != null ? (nearestEntity.getHeadPosition() - origin).normalized : player.GetLookVector();

                                        thrownWeaponMoveScript.Fire(origin, direction, player, __instance.hitmaskOverride, __instance.maxThrowStrength);
                                        ((Component)invData.model).gameObject.SetActive(true);
                                        GameObject.Destroy(thrownWeaponMoveScript.gameObject, __instance.LifeTime);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 辅助类，用于传递数据。 确保它是线程安全的！
        private class ThrowData {
            public float ThrowStrength { get; set; }
            public Vector3 PlayerPosition { get; set; }
            public Vector3 LookDirection { get; set; }
            public MyInventoryData ActionData { get; set; } //  小心使用，避免直接传递 Unity 对象
            public ItemActionThrownWeapon Instance { get; set; } // 小心使用，避免直接传递 Unity 对象
        }

        // 占位符方法 - 替换为你的实际逻辑
        private void FindNearbyEntities(EntityPlayerLocal player, float radius, System.Collections.Generic.List<EntityAlive> targets) {
            // 你的 FindNearbyEntities 逻辑
        }

        private EntityAlive GetNearestEntity(EntityPlayerLocal player, System.Collections.Generic.List<EntityAlive> targets) {
            // 你的 GetNearestEntity 逻辑
            return null; // 替换为实际返回值
        }
    }
}
