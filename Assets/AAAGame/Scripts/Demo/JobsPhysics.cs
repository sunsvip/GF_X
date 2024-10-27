using DG.Tweening;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;
using static CombatUnitEntity;

public class JobsPhysics
{
    public readonly static QueryParameters QueryParametersForPlayer = new QueryParameters(1 << LayerMask.NameToLayer("Enemy"));
    public readonly static QueryParameters QueryParametersForEnemy = new QueryParameters(1 << LayerMask.NameToLayer("Player"));
    /// <summary>
    /// 多线程搜索范围内的碰撞体
    /// </summary>
    /// <param name="selfCamp">自己的阵营</param>
    /// <param name="point">中心点</param>
    /// <param name="radius">半径</param>
    /// <param name="maxCount">最大返回碰撞体数量</param>
    /// <returns>返回碰撞体列表</returns>
    public static NativeArray<ColliderHit> OverlapSphere(CombatFlag selfCamp, Vector3[] points, float radius, int maxCount)
    {
        if (maxCount < 1) return default;
        int queryCount = points.Length;
        NativeArray<OverlapSphereCommand> commands = new NativeArray<OverlapSphereCommand>(queryCount, Allocator.TempJob);
        var campFlag = selfCamp == CombatFlag.Player ? QueryParametersForPlayer : QueryParametersForEnemy;
        for (int i = 0; i < queryCount; i++)
        {
            commands[i] = new OverlapSphereCommand(points[i], radius, campFlag);

        }
        NativeArray<ColliderHit> hitColliders = new NativeArray<ColliderHit>(maxCount * queryCount, Allocator.TempJob);
        OverlapSphereCommand.ScheduleBatch(commands, hitColliders, 1, maxCount).Complete();
        commands.Dispose();
        //NativeList<int> results = new NativeList<int>(maxCount, Allocator.TempJob);
        //for (int i = 0; i < hitColliders.Length; i++)
        //{
        //    var hitCollider = hitColliders[i];
        //    if (hitCollider.instanceID == 0) break;
        //    var hitTarget = hitCollider.collider.GetComponent<CombatUnitEntity>();
        //    if (hitTarget != null && hitTarget.Hp > 0)
        //        results.AddNoResize(hitTarget.Id);
        //}
        //hitColliders.Dispose();
        return hitColliders;
    }
    /// <summary>
    /// 获取最近的requireCount个目标
    /// </summary>
    /// <param name="selfCamp"></param>
    /// <param name="point"></param>
    /// <param name="radius"></param>
    /// <param name="perRequireCount"></param>
    /// <returns></returns>
    public static NativeArray<int> OverlapSphereNearest(CombatFlag selfCamp, Vector3[] points, float radius, int perRequireCount = -1)
    {
        float perUnitArea = math.PI * math.pow(0.5f, 2); //一个碰撞单位圆的面积
        float targetArea = math.PI * math.pow(radius, 2); //检测范围的面积
        int maxCount = Mathf.CeilToInt(targetArea / perUnitArea); //通过面积大致得到最大索敌个数,再根据距离筛选出最近的目标
        if (perRequireCount == -1) perRequireCount = maxCount;
        if (perRequireCount > 0)
        {
            var hitColliders = OverlapSphere(selfCamp, points, radius, maxCount);
            if (hitColliders.IsCreated)
            {
                var closestHits = FindClosestHits(points, hitColliders, perRequireCount);
                hitColliders.Dispose();
                return closestHits;
            }
        }
        return default;
    }
    private static NativeArray<int> FindClosestHits(Vector3[] points, NativeArray<ColliderHit> hits, int count)
    {
        int pointCount = points.Length;
        NativeArray<float> closestDistances = new NativeArray<float>(count, Allocator.Temp);
        NativeArray<int> closestHits = new NativeArray<int>(pointCount * count, Allocator.Temp);
        int perPointHitCount = hits.Length / pointCount;
        for (int queryIndex = 0; queryIndex < pointCount; queryIndex++)
        {
            int offsetIndex = queryIndex * count;
            var point = points[queryIndex];
            for (int i = 0; i < count; i++)
            {
                closestDistances[i] = float.MaxValue;
            }
            int startIdx = queryIndex * perPointHitCount;
            int endIdx = startIdx + perPointHitCount;
            for (int index = startIdx; index < endIdx; index++)
            {
                var hit = hits[index];
                if (hit.instanceID == 0) break;
                if (!hit.collider.TryGetComponent<CombatUnitEntity>(out var entity) || entity.Hp <= 0) continue;
                float distance = math.distancesq(entity.CachedTransform.position, point);

                for (int i = 0; i < count; i++)
                {
                    if (distance < closestDistances[i])
                    {
                        for (int j = count - 1; j > i; j--)
                        {
                            closestDistances[j] = closestDistances[j - 1];
                            closestHits[offsetIndex + j] = closestHits[offsetIndex + j - 1];
                        }
                        closestDistances[i] = distance;
                        closestHits[offsetIndex + i] = entity.Id;
                        break;
                    }
                }
            }
        }
        closestDistances.Dispose();
        return closestHits;
    }

    //public static void OverlapSphere(IList<CombatUnitEntity> combatUnits)
    //{
    //    if (combatUnits.Count == 0) return;
    //    NativeArray<OverlapSphereCommand> commands = new NativeArray<OverlapSphereCommand>(combatUnits.Count, Allocator.TempJob);
    //    int maxSearchTargetsCount = 1;
    //    for (int i = 0; i < combatUnits.Count; i++)
    //    {
    //        var unit = combatUnits[i];
    //        commands[i] = unit.SearchTargetsCommand;
    //        if (maxSearchTargetsCount < unit.SearchTargetsMaxCount)
    //        {
    //            maxSearchTargetsCount = unit.SearchTargetsMaxCount;
    //        }
    //    }
    //    NativeArray<ColliderHit> hitColliders = new NativeArray<ColliderHit>(maxSearchTargetsCount * combatUnits.Count, Allocator.TempJob);
    //    OverlapSphereCommand.ScheduleBatch(commands, hitColliders, 1, maxSearchTargetsCount).Complete();
    //    commands.Dispose();
    //    if (maxSearchTargetsCount == 1)
    //    {
    //        for (int i = 0; i < hitColliders.Length; i++)
    //        {
    //            var hitCollider = hitColliders[i];
    //            if (hitCollider.instanceID == 0) continue;
    //            var hitTarget = hitCollider.collider.GetComponent<CombatUnitEntity>();
    //            if (hitTarget != null)
    //            {
    //                var attacker = combatUnits[i];
    //                attacker.Attack(hitTarget);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        int entityIndex = 0;
    //        for (int i = 0; i < hitColliders.Length; i += maxSearchTargetsCount)
    //        {
    //            var attacker = combatUnits[entityIndex];
    //            for (int j = 0; j < maxSearchTargetsCount; j++)
    //            {
    //                var hitCollider = hitColliders[i + j];
    //                if (hitCollider.instanceID == 0) break;
    //                var hitTarget = hitCollider.collider.GetComponent<CombatUnitEntity>();
    //                if (hitTarget != null)
    //                {
    //                    attacker.Attack(hitTarget);
    //                }
    //            }
    //            entityIndex++;
    //        }
    //    }
    //    hitColliders.Dispose();
    //}
}
