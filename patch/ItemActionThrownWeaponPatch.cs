using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Audio;
using HarmonyLib;
using static ItemActionThrowAway;
using UnityEngine;
using System.Globalization;
using Random = UnityEngine.Random;
using Platform;
using NWS_WeaponSkill;



namespace XingChen.patch {
    [HarmonyPatch(typeof(ItemActionThrownWeapon))]
    public class ItemActionThrownWeaponPatch {

        [HarmonyPatch("throwAway")]
        [HarmonyPrefix]
        public static bool throwAwayPrefix(ItemActionThrownWeapon __instance, MyInventoryData _actionData) {
            if (_actionData != null) {
                ItemInventoryData invData = _actionData.invData;
                if (invData != null) {
                    ItemValue item = invData.itemValue;
                    if (item != null && item.ItemClass.HasAnyTags(WeaponTags.allTags)) {
                        if (invData.holdingEntity is EntityPlayerLocal player) {
                            float v = __instance.Properties.GetFloat("ThrownRadius");
                            List<EntityAlive> targets = new List<EntityAlive>();
                            FindNearbyEntities(player,v, targets);
                            v = __instance.Properties.GetFloat("ExSkillTriggerNumber");
                            //Log.Out($"c::{tCount}");
                            //throwInterval = __instance.Properties.GetFloat("ThrowInterval", 1f);
                            Log.Out($"now::{_actionData.m_ThrowStrength } max:: {__instance.maxThrowStrength} ");
                            if (targets.Count > v) {
                                int c = __instance.Properties.GetInt("ExSkillNumber");
                                //if (item.ItemClass.HasAnyTags(WeaponTags.plusTags)) c = int.MaxValue;
                                int cc = 0;
                                foreach (EntityAlive target in targets) {
                                    if (cc < c) {
                                        cc++;
                                        Vector3 targetHead = target.getHeadPosition();
                                        ThrownWeaponMoveScript thrownWeaponMoveScript = __instance.instantiateProjectile(_actionData);
                                        Vector3 origin = targetHead; // 直接从目标头顶发射
                                        Vector3 direction = (target.position - origin).normalized;
                                        thrownWeaponMoveScript.Fire(origin, direction, player, __instance.hitmaskOverride, __instance.maxThrowStrength);
                                        ((Component)invData.model).gameObject.SetActive(true);
                                        GameObject.Destroy(thrownWeaponMoveScript.gameObject, __instance.LifeTime);
                                    }
                                }
                            } else {
                                int c = __instance.Properties.GetInt("SkillCount"); ;
                                //if (item.ItemClass.HasAnyTags(WeaponTags.plusTags)) c = 9;
                                EntityAlive nearestEntity = targets.FirstOrDefault();
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
                        return false;
                    }
                }
            }
            return true;
        }

        private static List<EntityAlive> FindNearbyEntities(EntityPlayerLocal player, float range) {
            List<EntityAlive> entities = new List<EntityAlive>();
            foreach (Entity entity in GameManager.Instance.World.Entities.list) {
                if (entity is EntityAlive alive && !alive.IsDead() && !(alive is EntityPlayer) && !(alive is EntityNPC)) {
                    if (Vector3.Distance(player.position, alive.position) <= range) {
                        entities.Add(alive);
                    }
                }
            }
            return entities;
        }

        // 对象池 (用于 ThrownWeaponMoveScript)
        private static Stack<ThrownWeaponMoveScript> projectilePool = new Stack<ThrownWeaponMoveScript>();

        private static ThrownWeaponMoveScript GetProjectileFromPool(ItemActionThrownWeapon itemAction, MyInventoryData actionData) {
            ThrownWeaponMoveScript projectile;
            if (projectilePool.Count > 0) {
                projectile = projectilePool.Pop();
                projectile.gameObject.SetActive(true); // 激活
            } else {
                projectile = itemAction.instantiateProjectile(actionData);
                //projectile = GameObject.Instantiate(itemAction.ProjectilePrefab).GetComponent<ThrownWeaponMoveScript>(); //原始实例化
            }
            return projectile;
        }

        private static void ReturnProjectileToPool(ThrownWeaponMoveScript projectile) {
            projectile.gameObject.SetActive(false); // 禁用
            projectilePool.Push(projectile);
        }


        // 投掷任务 (在单独的线程中运行)
        private static async Task ThrowingTask(EntityPlayerLocal player, ItemActionThrownWeapon itemAction, ItemValue itemValue, float duration, float throwInterval, float radius, MyInventoryData actionData) {
            float startTime = Time.time;
            List<EntityAlive> targets = new List<EntityAlive>(); //缓存敌人列表

            while (Time.time - startTime < duration) {
                // 重新获取目标列表（或者你可以定期更新缓存的列表）
                targets.Clear();
                FindNearbyEntities(player, radius, targets);

                foreach (EntityAlive target in targets) {
                    if (target == null || !target.IsAlive()) continue;

                    // 计算投掷位置（敌人头部上方）
                    Vector3 targetPosition = target.getHeadPosition();
                    targetPosition.y += 20f;

                    // 获取投掷物
                    ThrownWeaponMoveScript projectile = GetProjectileFromPool(itemAction, actionData);

                    // 计算投掷方向
                    Vector3 origin = player.getHeadPosition();
                    Vector3 direction = (targetPosition - origin).normalized;

                    // 投掷
                    projectile.Fire(origin, direction, player, itemAction.hitmaskOverride, 1f); // 蓄力满，强度为 1

                    // 投掷后, 短暂延迟, 然后回收
                    // 注意: 这里需要用Task.Delay, 不能用Thread.Sleep, 否则会阻塞线程
                    Task.Run(async () => {
                        await Task.Delay(500); // 0.5秒后回收
                        ReturnProjectileToPool(projectile);
                    });
                }
                // 等待下一次投掷
                //Thread.Sleep((int)(throwInterval * 1000)); //不能用Thread.Sleep, 会阻塞线程
                await Task.Delay((int)(throwInterval * 1000));
            }
        }

        // 查找附近的实体 (更高效的实现)
        private static void FindNearbyEntities(EntityPlayerLocal player, float radius, List<EntityAlive> targets) {
            // 使用 SphereBounds 检查，更高效
            Bounds bounds = new Bounds(player.position, new Vector3(radius * 2f, radius * 2f, radius * 2f));
            List<Entity> entitiesInBounds = GameManager.Instance.World.GetEntitiesInBounds(player, bounds, true);
            //target.IsEnemy(player)
            foreach (Entity entity in entitiesInBounds) {
                if (entity is EntityAlive target && target != player && target.IsAlive() && !(target is EntityNPC)) {
                    targets.Add(target);
                }
            }
        }
        private static EntityAlive FindNearestEntityX(EntityPlayerLocal player, float radius) {
            float minDistance = radius; // 限制搜索范围
            EntityAlive nearestEntity = null;
            List<EntityAlive> entitiesInBounds = new List<EntityAlive>();
            FindNearbyEntities(player,radius, entitiesInBounds);
            entitiesInBounds.ForEach(entity => {
                if (!entity.IsDead() && !(entity is EntityPlayer)) { // 关键修改：排除所有 EntityPlayer
                    float distance = Vector3.Distance(player.position, entity.position);
                    if (distance < minDistance) {
                        minDistance = distance;
                        nearestEntity = entity;
                    }
                }

            });
            return nearestEntity;
        }
        private static EntityAlive FindNearestEntity(EntityPlayerLocal player,float minDistance) {
            //float minDistance = 33f; // 限制搜索范围
            EntityAlive nearestEntity = null;

            foreach (Entity entity in GameManager.Instance.World.Entities.list) {
                if (entity is EntityAlive alive && !alive.IsDead() && !(alive is EntityPlayer)) { // 关键修改：排除所有 EntityPlayer
                    float distance = Vector3.Distance(player.position, alive.position);
                    if (distance < minDistance) {
                        minDistance = distance;
                        nearestEntity = alive;
                    }
                }
            }
            return nearestEntity;
        }
    }
}
