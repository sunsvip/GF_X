using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
public class EntityBase : EntityLogic
{
    public int Id { get; private set; }
    public EntityParams Params { get; private set; }
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (userData != null) Params = userData as EntityParams;
        Id = this.Entity.Id;
    }

    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        Id = this.Entity.Id;
        if (userData != null)
        {
            Params = userData as EntityParams;
            if (Params.position != null)
            {
                this.CachedTransform.position = Params.position.Value;
            }
            if (Params.eulerAngles != null)
            {
                this.CachedTransform.eulerAngles = Params.eulerAngles.Value;
            }
            if (Params.localScale != null)
            {
                this.CachedTransform.localScale = Params.localScale.Value;
            }
            if (Params.layer != null)
            {
                var layerId = LayerMask.NameToLayer(Params.layer);
                gameObject.SetLayerRecursively(layerId);
            }
        }
    }
    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        if (!isShutdown && Params != null)
        {
            ReferencePool.Release(Params);
        }
    }
}
