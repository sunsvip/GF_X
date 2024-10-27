using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;

public class AIEnemyEntity : CombatUnitEntity
{
    public const string P_Target = "Target";
    Transform m_Target;
    Rigidbody m_Rigidbody;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_Rigidbody = GetComponent<Rigidbody>();
    }
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        m_Target = Params.Get<VarTransform>(P_Target);
    }
    private void FixedUpdate()
    {
        if (m_Target != null)
        {
            var offsetPos = m_Target.position - CachedTransform.position;
            offsetPos.y = 0;
            var moveDir = Vector3.Normalize(offsetPos);
            var targetVelocity = CombatUnitRow.MoveSpeed * moveDir;
            m_Rigidbody.velocity = Vector3.Lerp(m_Rigidbody.velocity, targetVelocity, 1 / math.distancesq(targetVelocity, m_Rigidbody.velocity));
        }
    }
    protected override bool ApplyDamage(CombatUnitEntity attacker, int damgeValue)
    {
        bool bekilled = base.ApplyDamage(attacker, damgeValue);
        if (Hp > 0)
        {
            m_Rigidbody.velocity = Vector3.Normalize(CachedTransform.position - attacker.CachedTransform.position) * 10f;
        }
        return bekilled;
    }
}
