using Cysharp.Threading.Tasks;
using GameFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public class Spawnner : MonoBehaviour
{
    [SerializeField] CombatUnitEntity.CombatFlag m_UnitFlag = CombatUnitEntity.CombatFlag.Enemy;
    [SerializeField] Vector2Int m_RowCol = new Vector2Int(10, 5);
    [SerializeField] Vector2 m_PosPadding = Vector2.one;
    [SerializeField] int m_CombatUnitId = 1;
    /// <summary>
    /// 玩家进入区域内开始刷兵
    /// </summary>
    [SerializeField] Vector2 m_TriggerBounds = Vector2.one;
    [SerializeField] int m_MaxSpawnCountPerFrame = 10;
    int m_SpawnCount;
    Bounds m_SpawnBounds;

    CombatUnitTable m_CombatUnitRow;
    private void Start()
    {
        InitValue();
        var combatUnitTb = GF.DataTable.GetDataTable<CombatUnitTable>();
        m_CombatUnitRow = combatUnitTb.GetDataRow(m_CombatUnitId);
    }
    private void InitValue()
    {
        m_SpawnCount = m_RowCol.x * m_RowCol.y;
        m_SpawnBounds = new Bounds(transform.position, new Vector3(m_TriggerBounds.x, 1, m_TriggerBounds.y));
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = m_SpawnCount - 1; i >= 0; i--)
        {
            var point = GetSpawnPoint(i);
            Gizmos.DrawWireCube(point, Vector3.one);
        }
        UnityEditor.Handles.Label(transform.position, Utility.Text.Format("CombatUnitId:{0}, SpawnCount:{1}", m_CombatUnitId, m_SpawnCount), UnityEditor.EditorStyles.whiteBoldLabel);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(m_SpawnBounds.center, m_SpawnBounds.size);
    }
    private void OnValidate()
    {
        InitValue();
    }
#endif
    public Vector3 GetSpawnPoint(int index)
    {
        if (index < 0 || index >= m_SpawnCount)
        {
            Debug.LogError("index error.");
            return Vector3.zero;
        }
        Vector2 halfSize = (m_RowCol - Vector2.one) * m_PosPadding * 0.5f;
        float x = index % m_RowCol.x;
        float z = index / m_RowCol.x;
        var point = new Vector3(x * m_PosPadding.x - halfSize.x, 0, z * m_PosPadding.y - halfSize.y);
        return transform.TransformPoint(point);
    }
    /// <summary>
    /// 检测玩家是否进入刷兵区域
    /// </summary>
    /// <param name="playerPos"></param>
    /// <returns></returns>
    public bool CheckInBounds(Vector3 playerPos)
    {
        return m_SpawnBounds.Contains(playerPos);
    }

    public IList<int> SpawnAllCombatUnits(PlayerEntity player)
    {
        int[] units = new int[m_SpawnCount];
        var entityEulerAngles = transform.eulerAngles;
        for (int i = m_SpawnCount - 1; i >= 0; i--)
        {
            var eParams = EntityParams.Create(GetSpawnPoint(i), entityEulerAngles);
            eParams.Set(AIEnemyEntity.P_DataTableRow, m_CombatUnitRow);
            eParams.Set<VarInt32>(AIEnemyEntity.P_CombatFlag, (int)m_UnitFlag);
            if (m_UnitFlag == CombatUnitEntity.CombatFlag.Enemy)
            {
                eParams.Set<VarTransform>(AIEnemyEntity.P_Target, player.CachedTransform);
            }
            int entityId = GF.Entity.ShowEntity<AIEnemyEntity>(m_CombatUnitRow.PrefabName, Const.EntityGroup.Player, eParams);
            units[i] = entityId;
            //if (i % m_MaxSpawnCountPerFrame == 0) await UniTask.DelayFrame(1);
        }
        return units;
    }
}
