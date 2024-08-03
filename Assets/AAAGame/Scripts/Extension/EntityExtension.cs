using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using TMPro;
public static class EntityExtension
{
    private static int m_EntityId = 0;
    /// <summary>
    /// 创建粒子特效
    /// </summary>
    /// <param name="eCom"></param>
    /// <param name="fxName">特效prefab, 相对路径MainGame/Entity/Effect/</param>
    /// <param name="spawnPos">特效位置</param>
    /// <param name="lifeTime">几秒后销毁粒子</param>
    /// <returns></returns>
    public static int ShowParticle(this EntityComponent eCom, string fxName, Vector3 spawnPos, float lifeTime = 3)
    {
        var fxParms = EntityParams.Acquire(spawnPos);
        fxParms.Set<VarFloat>("LifeTime", lifeTime);
        return eCom.ShowEntity<ParticleEntity>(Utility.Text.Format("Effect/{0}", fxName), Const.EntityGroup.Effect, fxParms);
    }
    public static void ShowPopEmoji(this EntityComponent eCom, string emojiName, Vector3 startPos, Vector3 endPos, float duration = 2)
    {
        var effectParms = EntityParams.Acquire(startPos, Vector3.zero, Vector3.one);
        VarObject onShowCb = ReferencePool.Acquire<VarObject>();
        onShowCb.Value = new GameFrameworkAction<EntityLogic>(entity =>
        {
            entity.transform.localScale = Vector3.zero;
            var seqAct = DOTween.Sequence();
            seqAct.Join(entity.transform.DOScale(1, duration));
            seqAct.Join(entity.transform.DOMove(endPos, duration));
            seqAct.SetEase(Ease.OutCubic);
            seqAct.AppendInterval(0.2f);
            seqAct.SetUpdate(true);
            seqAct.onComplete = () =>
            {
                GF.Entity.HideEntitySafe(entity);
            };
            seqAct.SetAutoKill();
        });
        effectParms.Set<VarObject>("OnShow", onShowCb);
        eCom.ShowEntity<ParticleEntity>(Utility.Text.Format("Effect/{0}", emojiName), Const.EntityGroup.Effect, effectParms);
    }
    public static void PopScreenText(this EntityComponent eCom, string text, Vector3 startWorldPos, float popDistance, float duration = 1f)
    {
        var vPos = Camera.main.WorldToViewportPoint(startWorldPos);
        var sPos = GF.UICamera.ViewportToWorldPoint(vPos);
        var ePos = sPos + Vector3.up * popDistance;
        var effectParms = EntityParams.Acquire(sPos, Vector3.zero, Vector3.one);
        VarObject onShowCb = ReferencePool.Acquire<VarObject>();
        onShowCb.Value = new GameFrameworkAction<EntityLogic>((EntityLogic entity) =>
        {
            var textMesh = entity.GetComponent<TextMeshPro>();
            textMesh.text = text;
            var txtCol = textMesh.color;
            txtCol.a = 1;
            textMesh.color = txtCol;
            //entity.transform.localScale = Vector3.zero;
            var seqAct = DOTween.Sequence();
            seqAct.Join(entity.transform.DOScale(1, duration));
            seqAct.Join(entity.transform.DOMove(ePos, duration));
            seqAct.Append(textMesh.DOFade(0, 0.25f));
            //seqAct.AppendInterval(0.2f);

            seqAct.SetUpdate(true);
            seqAct.onComplete = () =>
            {
                GF.Entity.HideEntitySafe(entity);
            };
            seqAct.SetAutoKill();
        });
        effectParms.Set("OnShow", onShowCb);
        eCom.ShowEntity<SampleEntity>("Effect/OutOfMoney", Const.EntityGroup.Effect, effectParms);
    }
    public static void ShowPopText(this EntityComponent eCom, Vector3 startPos, Vector3 endPos, string content, float duration = 1f, float fontSize = 10f)
    {
        var effectParms = EntityParams.Acquire(startPos, Vector3.zero, Vector3.one);
        var onShowCb = ReferencePool.Acquire<VarObject>();
        onShowCb.Value = new GameFrameworkAction<EntityLogic>(eLogic =>
        {
            var textMesh = eLogic.GetComponent<TextMeshPro>();
            textMesh.text = content;
            var txtCol = textMesh.color;
            txtCol.a = 1;
            textMesh.color = txtCol;
            textMesh.fontSize = fontSize;
            eLogic.transform.localScale = Vector3.zero;
            var seqAct = DOTween.Sequence();
            seqAct.Join(eLogic.transform.DOScale(1, duration));
            seqAct.Join(eLogic.transform.DOMove(endPos, duration));
            seqAct.Append(textMesh.DOFade(0, 0.25f));
            //seqAct.AppendInterval(0.2f);
            int eId = eLogic.Entity.Id;
            seqAct.SetUpdate(true);
            seqAct.onComplete = () =>
            {
                eCom.HideEntitySafe(eId);
            };
            seqAct.SetAutoKill();
        });
        effectParms.Set("OnShow", onShowCb);
        eCom.ShowEntity<BillboardEntity>("Effect/MoneyText", Const.EntityGroup.Effect, effectParms);
    }
    public static int ShowEntity(this EntityComponent eCom, string pfbName, string logicName, Const.EntityGroup eGroup, int priority, EntityParams parms = null)
    {
        var eId = UtilityBuiltin.GenerateEntityId();
        var assetFullName = UtilityBuiltin.AssetsPath.GetEntityPath(pfbName);
        eCom.ShowEntity(eId, Type.GetType(logicName), assetFullName, eGroup.ToString(), priority, parms);
        return eId;
    }
    public static int ShowEntity(this EntityComponent eCom, string pfbName, string logicName, Const.EntityGroup eGroup, EntityParams parms = null)
    {
        return eCom.ShowEntity(pfbName, logicName, eGroup, 0, parms);
    }
    public static int ShowEntity<T>(this EntityComponent eCom, string pfbName, Const.EntityGroup eGroup, int priority, EntityParams parms = null) where T : EntityLogic
    {
        var eId = UtilityBuiltin.GenerateEntityId();
        var assetFullName = UtilityBuiltin.AssetsPath.GetEntityPath(pfbName);
        eCom.ShowEntity<T>(eId, assetFullName, eGroup.ToString(), priority, parms);
        return eId;
    }
    public static int ShowEntity<T>(this EntityComponent eCom, string pfbName, Const.EntityGroup eGroup, EntityParams parms = null) where T : EntityLogic
    {
        return eCom.ShowEntity<T>(pfbName, eGroup, 0, parms);
    }
    public static void HideGroup(this EntityComponent eCom, string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            Log.Warning("Entity Group Is Null Or WhiteSpace");
            return;
        }
        var eGroup = eCom.GetEntityGroup(groupName);
        var all = eGroup.GetAllEntities();

        foreach (Entity e in all)
        {
            eCom.HideEntity(e);
        }
    }
    public static void HideEntitySafe(this EntityComponent eCom, int entityId)
    {
        if (eCom.HasEntity(entityId) || eCom.IsLoadingEntity(entityId))
        {
            eCom.HideEntity(entityId);
        }
    }
    public static void HideEntitySafe(this EntityComponent eCom, EntityLogic logic)
    {
        if (logic != null)
        {
            eCom.HideEntity(logic.Entity);
        }
        
    }
    public static T GetEntity<T>(this EntityComponent eCom, int eId) where T : EntityLogic
    {
        if (!eCom.HasEntity(eId)) return null;

        var eLogic = eCom.GetEntity(eId).Logic as T;
        return eLogic;
    }
}
