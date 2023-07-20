using GameFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

public class ParticleEntity : EntityBase
{
    bool autoHide;
    float lifeTime;
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        autoHide = true;
        gameObject.SetLayerRecursively(LayerMask.GetMask("TransparentFX"));

        lifeTime = Params.Get<VarFloat>("LifeTime", 2f);

        autoHide = lifeTime > 0;

        if (Params.Has("SortLayer"))
        {
            int pSortLayer = Params.Get<VarInt32>("SortLayer");
            SetParticlesSortLayer(pSortLayer);
        }
        if (Params.Has("OnShow"))
        {
            (Params.Get<VarObject>("OnShow").Value as GameFrameworkAction<EntityLogic>)?.Invoke(this);
        }

    }
    private void SetParticlesSortLayer(int layer)
    {
        var particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem item in particles)
        {
            var render = item.GetComponent<Renderer>();
            render.sortingOrder = layer;
        }
    }
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (!autoHide)
        {
            return;
        }
        lifeTime -= elapseSeconds;
        if (lifeTime <= 0)
        {
            GF.Entity.HideEntitySafe(this);
        }
    }
}
