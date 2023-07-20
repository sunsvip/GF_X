#pragma warning disable IDE1006 // 命名样式
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

public class EntityParams : RefParams
{
    const string KeyLocalPosition = "localPosition";
    const string KeyPosition = "position";
    const string KeyLocalEulerAngles = "localEulerAngles";
    const string KeyEulerAngles = "eulerAngles";
    const string KeyLocalScale = "localScale";
    const string KeyLayer = "layer";
    public static EntityParams Acquire(Vector3? position = null, Vector3? eulerAngles = null, Vector3? localScale = null)
    {
        var eParams = ReferencePool.Acquire<EntityParams>();
        eParams.CreateRoot();
        if (position != null) eParams.position = position.Value;
        if (eulerAngles != null) eParams.eulerAngles = eulerAngles.Value;
        if (localScale != null) eParams.localScale = localScale.Value;
        return eParams;
    }
    public VarVector3 position
    {
        get => Get<VarVector3>(KeyPosition);
        set
        {
            Set<VarVector3>(KeyPosition, value);
        }
    }
    public VarVector3 localPosition
    {
        get => Get<VarVector3>(KeyLocalPosition);
        set
        {
            Set<VarVector3>(KeyLocalPosition, value);
        }
    }
    public VarVector3 localEulerAngles
    {
        get => Get<VarVector3>(KeyLocalEulerAngles);
        set
        {
            Set<VarVector3>(KeyLocalEulerAngles, value);
        }
    }
    public VarVector3 eulerAngles
    {
        get => Get<VarVector3>(KeyEulerAngles);
        set
        {
            Set<VarVector3>(KeyEulerAngles, value);
        }
    }

    public VarVector3 localScale
    {
        get => Get<VarVector3>(KeyLocalScale);
        set
        {
            Set<VarVector3>(KeyLocalScale, value);
        }
    }
    public VarString layer
    {
        get => Get<VarString>(KeyLayer);
        set
        {
            Set<VarString>(KeyLayer, value);
        }
    }
}
#pragma warning restore IDE1006 // 命名样式
