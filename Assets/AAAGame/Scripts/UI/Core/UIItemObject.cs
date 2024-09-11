using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;
/// <summary>
/// UI Item对象类, 用于对象池复用item, 使用UIFormBase类的SpawnItem/UnspawnItem从对象池取用/归还Item
/// </summary>
public class UIItemObject : ObjectBase
{
#pragma warning disable IDE1006 // 命名样式
    public GameObject gameObject { get; private set; }
#pragma warning restore IDE1006 // 命名样式
    public static T Create<T>(GameObject itemInstance) where T : UIItemObject, new()
    {
        var instance = ReferencePool.Acquire<T>();
        instance.Initialize(itemInstance);
        instance.gameObject = itemInstance;
        instance.OnCreate(itemInstance);
        return instance;
    }
    protected override void Release(bool isShutdown)
    {
        if (gameObject == null)
        {
            return;
        }
        Object.Destroy(gameObject);
    }

    protected virtual void OnCreate(GameObject itemInstance)
    {

    }
}
