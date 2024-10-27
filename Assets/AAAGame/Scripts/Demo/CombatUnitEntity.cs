using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 战斗单位实体
/// </summary>
public class CombatUnitEntity : EntityBase
{
    public enum CombatFlag
    {
        Player,
        Enemy
    }
    public const string P_DataTableRow = "DataTableRow";
    public const string P_CombatFlag = "CombatFlag";
    private OverlapSphereCommand m_SearchTargetsCommand;
    public OverlapSphereCommand SearchTargetsCommand
    {
        get
        {
            m_SearchTargetsCommand.point = CachedTransform.position;
            return m_SearchTargetsCommand;
        }
    }
    /// <summary>
    /// 索敌数量
    /// </summary>
    public int SearchTargetsMaxCount => CombatUnitRow.MaxAttackCount;
    /// <summary>
    /// 阵营
    /// </summary>
    protected CombatFlag CampFlag { get; private set; }
    public CombatUnitTable CombatUnitRow { get; private set; }

    public virtual int Hp { get; protected set; }

    public virtual Vector3 HitPoint { get=>CachedTransform.position + Vector3.up; }

    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        CampFlag = (CombatFlag)Params.Get<VarInt32>(P_CombatFlag).Value;
        gameObject.layer = LayerMask.NameToLayer(CampFlag == CombatFlag.Player ? "Player" : "Enemy");
        CombatUnitRow = Params.Get(P_DataTableRow) as CombatUnitTable;
        Hp = CombatUnitRow.Hp;
        m_SearchTargetsCommand = new OverlapSphereCommand(CachedTransform.position, CombatUnitRow.AttackRadius, CampFlag == CombatFlag.Player ? JobsPhysics.QueryParametersForPlayer : JobsPhysics.QueryParametersForEnemy);
    }

    public virtual bool Attack(CombatUnitEntity unit)
    {
        return Attack(unit, CombatUnitRow.Damage);
    }

    internal bool Attack(CombatUnitEntity entity, int v)
    {
        return entity.ApplyDamage(entity, v);
    }

    protected virtual bool ApplyDamage(CombatUnitEntity attacker, int damgeValue)
    {
        if (Hp <= 0) return false;
        Hp -= damgeValue;
        var hitPoint = HitPoint;
        var bloodFxParms = EntityParams.Create(hitPoint);
        GF.Entity.ShowEffect("Effect/BloodExplosion", bloodFxParms, 1.5f);
        var damageFxParms = EntityParams.Create(hitPoint);
        GF.Entity.ShowPopText(damageFxParms, damgeValue.ToString(), hitPoint + Vector3.up, 0.5f, 7);
        if (Hp <= 0)
        {
            OnBeKilled();
            return true;
        }
        return false;
    }

    protected virtual void OnBeKilled()
    {
        GF.Entity.HideEntity(this.Entity);
    }

    internal void SetColor(Color green)
    {
        GetComponent<Renderer>().material.color = green;
    }
}
