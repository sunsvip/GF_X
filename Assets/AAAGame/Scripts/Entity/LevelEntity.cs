using GameFramework;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public class LevelEntity : EntityBase
{
    public const string P_LevelData = "LevelData";
    public const string P_LevelReadyCallback = "OnLevelReady";
    public bool IsAllReady { get; private set; }
    private Transform playerSpawnPoint;
    PlayerEntity m_PlayerEntity;

    List<Spawnner> m_Spawnners;

    HashSet<int> m_EntityLoadingList;
    Dictionary<int, CombatUnitEntity> m_Enemies;
    bool m_IsGameOver;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        playerSpawnPoint = transform.Find("PlayerSpawnPoint");
        m_Spawnners = new List<Spawnner>();
        m_EntityLoadingList = new HashSet<int>();
        m_Enemies = new Dictionary<int, CombatUnitEntity>();
    }
    protected override async void OnShow(object userData)
    {
        base.OnShow(userData);
        GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Subscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
        m_PlayerEntity = null;
        m_IsGameOver = false;
        IsAllReady = false;
        m_Spawnners.Clear();
        m_EntityLoadingList.Clear();
        m_Enemies.Clear();
        CachedTransform.Find("EnemySpawnPoints").GetComponentsInChildren<Spawnner>(m_Spawnners);

        var combatUnitTb = GF.DataTable.GetDataTable<CombatUnitTable>();
        var playerRow = combatUnitTb.GetDataRow(0);
        var playerParams = EntityParams.Create(playerSpawnPoint.position, playerSpawnPoint.eulerAngles);
        playerParams.Set(PlayerEntity.P_DataTableRow, playerRow);
        playerParams.Set<VarInt32>(PlayerEntity.P_CombatFlag, (int)CombatUnitEntity.CombatFlag.Player);
        playerParams.Set<VarAction>(PlayerEntity.P_OnBeKilled, (Action)OnPlayerBeKilled);
        m_PlayerEntity = await GF.Entity.ShowEntityAwait<PlayerEntity>(playerRow.PrefabName, Const.EntityGroup.Player, playerParams) as PlayerEntity;
        CameraController.Instance.SetFollowTarget(m_PlayerEntity.CachedTransform);
        IsAllReady = true;
    }


    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (m_IsGameOver || !IsAllReady) return;
        SpawnEnemiesUpdate();
    }
    protected override void OnHide(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Unsubscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);

        base.OnHide(isShutdown, userData);
    }

    public void StartGame()
    {
        m_PlayerEntity.Ctrlable = true;
    }
    private void SpawnEnemiesUpdate()
    {
        if (m_Spawnners.Count == 0) return;
        Spawnner item = null;
        var playerPos = m_PlayerEntity.CachedTransform.position;
        for (int i = m_Spawnners.Count - 1; i >= 0; i--)
        {
            item = m_Spawnners[i];
            if (item.CheckInBounds(playerPos))
            {
                var ids = item.SpawnAllCombatUnits(m_PlayerEntity);
                m_Spawnners.RemoveAt(i);
                foreach (var entityId in ids)
                {
                    m_EntityLoadingList.Add(entityId);
                }
            }
        }
    }

    private void OnPlayerBeKilled()
    {
        if (m_IsGameOver) return;
        m_IsGameOver = true;
        var eParms = RefParams.Create();
        eParms.Set<VarBoolean>("IsWin", false);
        GF.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
    }
    private void CheckGameOver()
    {
        if(m_IsGameOver) return;
        if (m_Spawnners.Count < 1 && m_EntityLoadingList.Count < 1 && m_Enemies.Count < 1)
        {
            m_IsGameOver = true;
            var eParms = RefParams.Create();
            eParms.Set<VarBoolean>("IsWin", true);
            GF.Event.Fire(GameplayEventArgs.EventId, GameplayEventArgs.Create(GameplayEventType.GameOver, eParms));
        }
    }
    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var eArgs = e as ShowEntitySuccessEventArgs;
        int entityId = eArgs.Entity.Id;
        if (m_EntityLoadingList.Contains(entityId))
        {
            m_Enemies.Add(entityId, eArgs.Entity.Logic as CombatUnitEntity);
            m_EntityLoadingList.Remove(entityId);
        }
    }


    private void OnHideEntityComplete(object sender, GameEventArgs e)
    {
        var eArgs = e as HideEntityCompleteEventArgs;
        int entityId = eArgs.EntityId;
        if (m_Enemies.ContainsKey(entityId))
        {
            m_Enemies.Remove(entityId);
        }
        else if (m_EntityLoadingList.Contains(entityId))
        {
            m_EntityLoadingList.Remove(entityId);
        }

        CheckGameOver();
    }
}
