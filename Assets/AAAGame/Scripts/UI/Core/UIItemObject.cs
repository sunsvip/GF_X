using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;

public class UIItemObject : ObjectBase
{
#pragma warning disable IDE1006 // 命名样式
    public GameObject gameObject { get; private set; }
    public UIItemBase itemLogic { get; private set; }
#pragma warning restore IDE1006 // 命名样式
    public static T Create<T>(GameObject itemInstance) where T : UIItemObject, new()
    {
        var instance = ReferencePool.Acquire<T>();
        instance.Initialize(itemInstance);
        instance.gameObject = itemInstance;
        instance.itemLogic = itemInstance.GetComponent<UIItemBase>();
        instance.OnInit();
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

    protected virtual void OnInit() { }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        var transform = gameObject.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(true);
    }
    protected override void OnUnspawn()
    {
        base.OnUnspawn();
        gameObject.SetActive(false);
    }
}
