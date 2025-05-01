using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;

public class PlayerEntity : CombatUnitEntity
{
    public const string P_OnBeKilled = "OnBeKilled";
    const string EnemyTag = "Enemy";
    const string ANIM_MOVE_KEY = "Move";
    private Vector3 joystickForward;
    private float moveSpeed = 10f;
    private float rotationSpeed = 10f;
    CharacterController characterCtrl;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private Vector3 moveStep;

    private float m_AttackInterval = 0.2f;
    private float m_AttackTimer;
    private bool mCtrlable;
    public bool Ctrlable
    {
        get => mCtrlable;
        set
        {
            mCtrlable = value;
            GF.StaticUI.Joystick.Enable = mCtrlable;
        }
    }

    private PlayerDataModel m_PlayerData;
    Animator m_Animator;
    float m_DamageTimer;
    float m_DamageInterval = 0.25f;

    Action m_OnPlayerBeKilled = null;
    float m_HandIkWeight, m_IkSmooth = 10f;
    Vector3[] m_SmoothHandIkPoints;//双手IK Points
    CombatUnitEntity[] m_AttackTargets;
    Vector3[] m_HandTargetPoints;

    Transform[] m_Hands;
    Vector3[] m_HandPoints;
    SpriteRenderer m_SkilCircle;
    float m_SkillDiameter;
    Vector3[] m_SkillQueryPoints;
    protected Vector3[] HandPoints
    {
        get
        {
            for (int i = 0; i < m_HandPoints.Length; i++)
            {
                m_HandPoints[i] = m_Hands[i].position;
            }
            return m_HandPoints;
        }
    }
    public override int Hp { get => m_PlayerData.Hp; protected set => m_PlayerData.Hp = value; }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (CombatUnitRow == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(CachedTransform.position, CombatUnitRow.AttackRadius);
    }
#endif
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        characterCtrl = GetComponent<CharacterController>();
        m_Animator = GetComponent<Animator>();
        m_PlayerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        m_SmoothHandIkPoints = new Vector3[2];
        m_HandTargetPoints = new Vector3[2];
        m_Hands = new Transform[2];
        m_HandPoints = new Vector3[2];
        var hands = CachedTransform.Find("Hands");
        for (int i = 0; i < hands.childCount; i++)
        {
            m_Hands[i] = hands.GetChild(i);
        }
        m_SkilCircle = CachedTransform.Find("Circle").GetComponent<SpriteRenderer>();
        m_SkillQueryPoints = new Vector3[1];
    }
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        m_Animator.SetBool("BeKilled", false);
        moveSpeed = CombatUnitRow.MoveSpeed;
        m_PlayerData.SetData(PlayerDataType.Hp, CombatUnitRow.Hp);
        m_OnPlayerBeKilled = Params.Get<VarAction>(P_OnBeKilled);
        m_AttackTargets = new CombatUnitEntity[2];
        m_SkillDiameter = 10;
        m_SkilCircle.size = Vector2.one * m_SkillDiameter;
        GF.StaticUI.Joystick.OnPointerUpCallback += OnJoystickUp;
    }
    protected override void OnHide(bool isShutdown, object userData)
    {
        GF.StaticUI.Joystick.OnPointerUpCallback -= OnJoystickUp;
        base.OnHide(isShutdown, userData);
    }
    private void OnJoystickUp()
    {
        SkillAttack();
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (!Ctrlable) return;
        isGrounded = characterCtrl.isGrounded;

        Move(elapseSeconds);//移动
        AttackLogicUpdate(elapseSeconds);
        //Jump(elapseSeconds);//跳跃
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag(EnemyTag)) return;
        var enemey = collision.gameObject.GetComponent<CombatUnitEntity>();
        enemey.Attack(this);
    }
    private void OnCollisionStay(Collision collision)
    {
        if (!collision.collider.CompareTag(EnemyTag) || (m_DamageTimer += Time.deltaTime) < m_DamageInterval) return;
        var enemey = collision.gameObject.GetComponent<CombatUnitEntity>();
        enemey.Attack(this);
        m_DamageTimer = 0;
    }

    private void AttackLogicUpdate(float elapseSeconds)
    {
        //刷新攻击目标
        var nearestTargets = JobsPhysics.OverlapSphereNearest(CampFlag, HandPoints, CombatUnitRow.AttackRadius, CombatUnitRow.MaxAttackCount);
        RefreshTargets(nearestTargets, true);
        if (m_AttackTargets[0] != null && (m_AttackTimer += elapseSeconds) > m_AttackInterval)
        {
            m_AttackTimer = 0;
            for (int i = 0; i < m_AttackTargets.Length; i++)
            {
                var target = m_AttackTargets[i];
                ShootFx(m_Hands[i].position, target.HitPoint);
                Attack(target);
            }
        }
    }
    public override bool Attack(CombatUnitEntity unit)
    {
        bool bekilled = base.Attack(unit);
        if (bekilled)
        {
            AddSkillCircleDiameter(unit.CombatUnitRow.Id);
        }
        return bekilled;
    }
    private void AddSkillCircleDiameter(int v)
    {
        SetSkillCircleDiameter(m_SkillDiameter += v * 0.25f);
    }
    private void SetSkillCircleDiameter(float v)
    {
        m_SkillDiameter = v;
        m_SkilCircle.size = Vector2.one * m_SkillDiameter;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        bool hasTarget = m_AttackTargets[0] != null;
        float targetIkWeight = hasTarget ? 1 : 0;
        m_HandIkWeight = Unity.Mathematics.math.lerp(m_HandIkWeight, targetIkWeight, Time.deltaTime * m_IkSmooth);

        m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, targetIkWeight);
        m_Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, targetIkWeight);
        m_SmoothHandIkPoints[0] = Unity.Mathematics.math.lerp(m_SmoothHandIkPoints[0], m_HandTargetPoints[0], Time.deltaTime * m_IkSmooth);
        m_SmoothHandIkPoints[1] = Unity.Mathematics.math.lerp(m_SmoothHandIkPoints[1], m_HandTargetPoints[1], Time.deltaTime * m_IkSmooth);
        m_Animator.SetIKPosition(AvatarIKGoal.LeftHand, m_SmoothHandIkPoints[0]);
        m_Animator.SetIKPosition(AvatarIKGoal.RightHand, m_SmoothHandIkPoints[1]);
    }

    private void RefreshTargets(NativeArray<int> ids, bool dispose = true)
    {
        if (ids.IsCreated)
        {
            for (int i = 0; i < m_AttackTargets.Length; i++)
            {
                if (i < ids.Length && GF.Entity.HasEntity(ids[i]))
                {
                    m_AttackTargets[i] = GF.Entity.GetEntity<CombatUnitEntity>(ids[i]);
                }
                else
                {
                    m_AttackTargets[i] = null;
                }
            }
            if (dispose)
            {
                ids.Dispose();
            }
            if ((m_AttackTargets[0] ??= m_AttackTargets[1]) != null)
                m_HandTargetPoints[0] = m_AttackTargets[0].HitPoint;
            if ((m_AttackTargets[1] ??= m_AttackTargets[0]) != null)
                m_HandTargetPoints[1] = m_AttackTargets[1].HitPoint;
        }
    }
    private void Move(float elapseSeconds)
    {
        float movePower = GF.StaticUI.Joystick.Distance;
        m_Animator.SetFloat(ANIM_MOVE_KEY, movePower);
        joystickForward.Set(GF.StaticUI.Joystick.Horizontal, 0, GF.StaticUI.Joystick.Vertical);
        if (m_AttackTargets[0] != null)
        {
            var selfPos = CachedTransform.position;
            var halfPoint = (m_HandTargetPoints[0] + m_HandTargetPoints[1]) * 0.5f;
            halfPoint.y = selfPos.y = 0;
            var dir = (halfPoint - selfPos).normalized;
            if (!dir.Equals(Vector3.zero))
            {
                CachedTransform.rotation = Unity.Mathematics.math.slerp(CachedTransform.rotation, Unity.Mathematics.quaternion.LookRotation(dir, Vector3.up), elapseSeconds * rotationSpeed);
            }
        }
        else if (movePower > 0.001f)
        {
            CachedTransform.forward = Vector3.Slerp(CachedTransform.forward, joystickForward, elapseSeconds * rotationSpeed);
        }

        if (isGrounded)
        {
            if (playerVelocity.y < 0) playerVelocity.y = 0;
            moveStep = moveSpeed * movePower * joystickForward;
        }
        else
        {
            moveStep.y += Physics.gravity.y * elapseSeconds;
        }
        characterCtrl.Move(moveStep * elapseSeconds);
    }

    private async void SkillAttack()
    {
        float queryRadius = m_SkillDiameter * 0.5f;
        SetSkillCircleDiameter(1);
        m_SkillQueryPoints[0] = CachedTransform.position;
        var hitsList = JobsPhysics.OverlapSphereNearest(this.CampFlag, m_SkillQueryPoints, queryRadius);

        int entityId = 0;
        List<int> entityIds = new List<int>();
        CombatUnitEntity entity = null;
        for (int i = 0; i < hitsList.Length; i++)
        {
            entityId = hitsList[i];
            if (entityId == 0) break;
            entityIds.Add(entityId);
        }
        hitsList.Dispose();
        float damageInterval = 0.5f;
        float lastDamageRadius = damageInterval;
        bool hasEnemy = false;
        for (int i = 0; i < entityIds.Count; i++)
        {
            entityId = entityIds[i];
            if (!GF.Entity.HasEntity(entityId)) continue;
            entity = GF.Entity.GetEntity<CombatUnitEntity>(entityId);

            float distance = Vector3.Distance(CachedTransform.position, entity.CachedTransform.position);
            if (distance < lastDamageRadius)
            {
                Attack(entity, 100);
                hasEnemy = true;
            }
            else
            {
                lastDamageRadius += damageInterval;
                if (hasEnemy)
                {
                    await UniTask.DelayFrame(1);
                    hasEnemy = false;
                }
                i--;
            }
        }
    }

    protected override void OnBeKilled()
    {
        Ctrlable = false;
        m_Animator.SetBool("BeKilled", true);
        m_OnPlayerBeKilled.Invoke();
    }

    private void ShootFx(Vector3 position, Vector3 hitPoint)
    {
        var fxParams = EntityParams.Create(position, Quaternion.LookRotation(Vector3.Normalize(hitPoint - position)).eulerAngles);
        float duration = Vector3.Distance(position, hitPoint) * 0.01f;
        fxParams.OnShowCallback = SetShootParticleDuration;
        GF.Entity.ShowEffect("Effect/FireFx", fxParams, duration);
    }

    private void SetShootParticleDuration(EntityLogic obj)
    {
        var fx = obj.GetComponent<ParticleSystem>();
        var fxSettings = fx.main;
        fxSettings.duration = (obj as ParticleEntity).LifeTime;
        fx.Play(true);
    }
}
