using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
public class SampleEntity : EntityBase
{
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        if (Params.Has("OnShow"))
        {
            (Params.Get<VarObject>("OnShow").Value as GameFrameworkAction<EntityLogic>)?.Invoke(this);
        }
        if (Params.Has("AttachTo"))
        {
            Entity attachEntity = Params.Get<VarUnityObject>("AttachTo").Value as Entity;
            GF.Entity.AttachEntity(this.Entity, attachEntity);
        }
    }
}
