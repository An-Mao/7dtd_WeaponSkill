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
using SteelSeries.GameSense;
using UAI;
using static vp_ItemPickup;
using System.Threading;



namespace NWS_WeaponSkill.patch {
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
                            Task.Run(() => {

                                float v = __instance.Properties.GetFloat("ThrownRadius");
                                //Log.Out($"now :{_actionData.m_ThrowStrength} max :{ __instance.maxThrowStrength}");
                                if (item.ItemClass.HasAnyTags(WeaponTags.plusTags) && _actionData.m_ThrowStrength >= __instance.maxThrowStrength / 2) {
                                    float duration = __instance.Properties.GetFloat("MxSkillDuration"); // 默认持续时间 5 秒
                                    float throwInterval = __instance.Properties.GetFloat("MxSkillInterval"); // // 默认投掷间隔 1 秒
                                    if(_actionData.m_ThrowStrength >= __instance.maxThrowStrength) {
                                        MaxSkill(player, __instance, duration, throwInterval, v, _actionData, invData);
                                    }else{
                                        MxSkill(player, __instance, duration, throwInterval, v, _actionData, invData);
                                    }

                                    //float radius = 30f; //作用范围
                                    //
                                } else {
                                    List<EntityAlive> targets = new List<EntityAlive>();
                                    FindNearbyEntities(player, v, targets);
                                    v = __instance.Properties.GetFloat("ExSkillTriggerNumber");
                                    if (targets.Count <= v) {
                                        UsualSkill(__instance, _actionData, player, GetNearestEntity(player, targets), invData);
                                    } else {
                                        ExSkill(__instance, _actionData, player, targets, invData);
                                    }
                                }
                            });
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        private static List<Vector3> GenerateRandomTargetPositions(Vector3 center, float radius, float yOffset, float horizontalSpread, int count) {
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < count; i++) {
                // 在水平范围内随机生成位置
                float x = center.x + UnityEngine.Random.Range(-horizontalSpread, horizontalSpread);
                float z = center.z + UnityEngine.Random.Range(-horizontalSpread, horizontalSpread);

                // 确保随机位置在指定半径内
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * radius;
                x = center.x + randomPoint.x;
                z = center.z + randomPoint.y;

                positions.Add(new Vector3(x, center.y + yOffset, z));
            }
            return positions;
        }
        public static void MxSkill(EntityPlayerLocal player,
            ItemActionThrownWeapon __instance,
            float duration, float throwInterval, float radius,
            MyInventoryData actionData, ItemInventoryData invData) {
            float startTime = Time.time;
            float horizontalSpread = 5f;
            float verticalDirection = -1;
            while (Time.time - startTime < duration) {
                List<Vector3> targetPositions = GenerateRandomTargetPositions(player.position, radius, throwInterval, horizontalSpread, 10); // 生成多个随机目标位置

                foreach (Vector3 targetPosition in targetPositions) {
                    Vector3 direction = new Vector3(0, verticalDirection, 0).normalized; // 垂直向下

                    NWS_WeaponSkill._mainThreadContext.Post((state) =>
                    {
                        try {
                            ThrownWeaponMoveScript projectile = __instance.instantiateProjectile(actionData);
                            projectile.Fire(targetPosition, direction, player, __instance.hitmaskOverride, __instance.maxThrowStrength);
                            ((Component)invData.model).gameObject.SetActive(true);
                            RemoveOject(projectile, __instance.LifeTime);

                        } catch (Exception e) {
                            Log.Out("Error during projectile instantiation or firing: " + e.Message);
                        }
                    }, null);
                }

                Thread.Sleep((int)(throwInterval * 1000));
            }
        }
        private static void MaxSkill(EntityPlayerLocal player, 
            ItemActionThrownWeapon __instance, 
            float duration, float throwInterval, float radius,
            MyInventoryData actionData, ItemInventoryData invData) {
            float yOffset = __instance.Properties.GetFloat("MxSkillYOffset");
            float startTime = Time.time;
            while (Time.time - startTime < duration) {
                List<EntityAlive> targets = new List<EntityAlive>();
                FindNearbyEntities(player, radius, targets);
                foreach (EntityAlive target in targets) {
                    if (target == null || !target.IsAlive()) continue;
                    Vector3 targetPosition = target.getHeadPosition();
                    //targetPosition.y += yOffset;
                    Vector3 direction = (target.position - targetPosition).normalized;
                    NWS_WeaponSkill._mainThreadContext.Post((state) => {
                        ThrownWeaponMoveScript projectile = __instance.instantiateProjectile(actionData);
                        projectile.Fire(targetPosition, direction, player, __instance.hitmaskOverride, __instance.maxThrowStrength);
                        ((Component)invData.model).gameObject.SetActive(true);
                        RemoveOject(projectile, __instance.LifeTime);
                    }, null);

                }
                Thread.Sleep((int)(throwInterval * 1000));
            }
        }
        // 生成圆形扩散图案的位置
        public static List<Vector3> GenerateCircularPositions(Vector3 center, float radius, int count, float angleOffset) {
            List<Vector3> positions = new List<Vector3>();
            float angleIncrement = 360f / count; // 每个投掷物的角度增量

            for (int i = 0; i < count; i++) {
                float angle = i * angleIncrement + angleOffset; // 加上角度偏移
                float radian = angle * Mathf.Deg2Rad;

                // 计算圆形上的位置
                float x = center.x + radius * Mathf.Cos(radian);
                float z = center.z + radius * Mathf.Sin(radian);
                float y = center.y; // 保持在同一高度

                positions.Add(new Vector3(x, y, z));
            }

            return positions;
        }
        private static void UsualSkillX(ItemActionThrownWeapon __instance, ItemActionThrowAway.MyInventoryData _actionData, EntityPlayerLocal player, ItemInventoryData invData, float radius, int count, float yOffset, float angleOffset) {
            List<Vector3> circularPositions = GenerateCircularPositions(player.position, radius, count, angleOffset);

            foreach (Vector3 position in circularPositions) {
                NWS_WeaponSkill._mainThreadContext.Post((state) =>
                {
                    try {
                        ThrownWeaponMoveScript thrownWeaponMoveScript = __instance.instantiateProjectile(_actionData);

                        // 计算投掷方向 (你可以根据需要修改)
                        Vector3 origin = player.GetLookRay().origin;
                        Vector3 direction = (position + new Vector3(0, yOffset, 0) - origin).normalized;

                        thrownWeaponMoveScript.Fire(origin, direction, player, __instance.hitmaskOverride, __instance.maxThrowStrength);
                        ((Component)invData.model).gameObject.SetActive(true);

                        thrownWeaponMoveScript.gameObject.SetActive(false);
                        RemoveOject(thrownWeaponMoveScript, __instance.LifeTime);
                    } catch (Exception e) {
                        Log.Error("Error during projectile instantiation or firing: " + e.Message);
                    }
                }, null);
            }
        }
        private static void UsualSkill(ItemActionThrownWeapon __instance, MyInventoryData _actionData, EntityPlayerLocal player, EntityAlive nearestEntity, ItemInventoryData invData) {
            int c = __instance.Properties.GetInt("SkillCount");
            for (int i = 0; i < c; i++) {
                NWS_WeaponSkill._mainThreadContext.Post((state) => {
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
                    RemoveOject(thrownWeaponMoveScript, __instance.LifeTime);
                }, null);
            }
        }
        private static void ExSkill(ItemActionThrownWeapon __instance, MyInventoryData _actionData, EntityPlayerLocal player, List<EntityAlive> targets, ItemInventoryData invData) {
            int c = __instance.Properties.GetInt("ExSkillNumber");
            int cc = 0;
            foreach (EntityAlive target in targets) {
                if (cc < c) {
                    cc++;
                    Vector3 targetHead = target.getHeadPosition();
                    NWS_WeaponSkill._mainThreadContext.Post((state) => {
                        ThrownWeaponMoveScript thrownWeaponMoveScript = __instance.instantiateProjectile(_actionData);
                        Vector3 origin = targetHead;
                        Vector3 direction = (target.position - origin).normalized;
                        thrownWeaponMoveScript.Fire(origin, direction, player, __instance.hitmaskOverride, __instance.maxThrowStrength);
                        ((Component)invData.model).gameObject.SetActive(true);
                        RemoveOject(thrownWeaponMoveScript, __instance.LifeTime);

                    }, null);
                }
            }

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
            List<EntityAlive> entitiesInBounds = new List<EntityAlive>();
            FindNearbyEntities(player,radius, entitiesInBounds);
            return GetNearestEntity(player,entitiesInBounds);
        }

        private static EntityAlive GetNearestEntity(EntityPlayerLocal player, List<EntityAlive> entityAlives) {
            float minDistance = float.MaxValue;
            EntityAlive nearestEntity = null;
            entityAlives.ForEach(entity => {
                if (!entity.IsDead() && !(entity is EntityPlayer)) {
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
        // 对象池 (用于 ThrownWeaponMoveScript)
        private static Stack<ThrownWeaponMoveScript> projectilePool = new Stack<ThrownWeaponMoveScript>();

        private static ThrownWeaponMoveScript GetProjectileFromPool(ItemActionThrownWeapon itemAction, ItemActionThrowAway.MyInventoryData actionData) {
            ThrownWeaponMoveScript projectile = null;
            if (projectilePool.Count > 0) {
                projectile = projectilePool.Pop();
                projectile.gameObject.SetActive(true); // 激活

            } else {
                projectile = itemAction.instantiateProjectile(actionData);
            }
            return projectile;
        }

        private static void ReturnProjectileToPool(ThrownWeaponMoveScript projectile) {
            projectile.gameObject.SetActive(false); // 禁用
            projectilePool.Push(projectile);
        }

        private static void RemoveOject(ThrownWeaponMoveScript thrown, float t) {
            //thrown.gameObject.SetActive(false);
            GameManager.Destroy(thrown.gameObject, t);
            //GameManager.DestroyImmediate(thrown.gameObject);
        }
    }

}
