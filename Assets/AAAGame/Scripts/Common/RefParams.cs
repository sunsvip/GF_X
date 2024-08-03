
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityGameFramework.Runtime;
/// <summary>
/// 引用类型参数, 用于Entity/UI传递参数,规避new
/// </summary>
public class RefParams : IReference
{
    public int Id { get; protected set; }
    public static RefParams Acquire()
    {
        var eParams = ReferencePool.Acquire<RefParams>();
        eParams.CreateRoot();
        return eParams;
    }
    /// <summary>
    /// 创建数据根节点
    /// </summary>
    protected void CreateRoot()
    {
        this.Id = UtilityBuiltin.GenerateEntityId();
    }

    public void Set<T>(string key, T value) where T : Variable
    {
        GF.VariablePool.SetVariable<T>(Id, key, value);
    }
    public void Set(string key, object value)
    {
        var varObj = ReferencePool.Acquire<VarObject>();
        varObj.Value = value;
        Set<VarObject>(key, varObj);
    }
    /// <summary>
    /// 获取引用类型的参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public T Get<T>(string key, T defaultValue = null) where T : Variable
    {
        if (defaultValue != null)
        {
            _ = UniTask.DelayFrame(1).ContinueWith(() =>
            {
                ReferencePool.Release(defaultValue);
            });

        }

        return GF.VariablePool.GetVariable<T>(Id, key) ?? defaultValue;
    }

    public bool TryGet<T>(string key, out T value) where T : Variable
    {
        return GF.VariablePool.TryGetVariable<T>(this.Id, key, out value);
    }

    public void Clear()
    {
        ClearDirtyData();
        GF.VariablePool.ClearVariables(this.Id);
    }
    /// <summary>
    /// 释放时回调, 需重写此方法重置数据以避免脏数据
    /// </summary>
    protected virtual void ClearDirtyData()
    {

    }
}
