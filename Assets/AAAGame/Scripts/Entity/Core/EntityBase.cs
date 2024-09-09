using GameFramework;
using UnityGameFramework.Runtime;
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(EntityBase), true)]
public class EntityBaseInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!EditorApplication.isPlaying) return;

        EditorGUILayout.SelectableLabel($"EntityId: {(target as EntityBase).Id}");
    }
}
#endif
public class EntityBase : EntityLogic
{
    public int Id { get; private set; }
    public EntityParams Params { get; private set; }
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (userData == null)
        {
            Log.Error("创建Entity失败! 你必须为Entity传入EntityParams数据");
        }
        Params = userData as EntityParams;
        Id = this.Entity.Id;
    }

    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        Id = this.Entity.Id;
        if (userData == null)
        {
            Log.Error("创建Entity失败! 你必须为Entity传入EntityParams数据");
            return;
        }
        Params = userData as EntityParams;
        if (GF.Entity.IsValidEntity(Params.AttchToEntity))
        {
            GF.Entity.AttachEntity(this.Entity, Params.AttchToEntity, Params.ParentTransform);
        }
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
        if (Params.gameObjectLayer >= 0)
        {
            gameObject.layer = Params.gameObjectLayer;
            //gameObject.SetLayerRecursively(Params.gameObjectLayer);
        }

        Params.OnShowCallback?.Invoke(this);
    }
    protected override void OnHide(bool isShutdown, object userData)
    {
        Params.OnHideCallback?.Invoke(this);
        base.OnHide(isShutdown, userData);
        if (!isShutdown && Params != null)
        {
            ReferencePool.Release(Params);
        }
    }
}
