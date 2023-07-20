
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.DataNode;
using UnityGameFramework.Runtime;
/// <summary>
/// 引用类型参数, 用于Entity/UI传递参数,规避new
/// </summary>
public class RefParams : IReference
{
    static int _instanceId = 0;
    protected IDataNode RootNode { get; private set; }
    protected string RootNodeName { get; private set; }

    /// <summary>
    /// 创建数据根节点
    /// </summary>
    protected void CreateRoot()
    {
        RootNode = GF.DataNode.GetOrAddNode(Utility.Text.Format("PDN_{0}", ++_instanceId));
        RootNodeName = RootNode.FullName;
    }

    public void Set<T>(string key, T value) where T : Variable
    {
        var node = RootNode.GetOrAddChild(key);
        node.SetData(value);
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
        if(defaultValue != null)
        {
            _ = UniTask.RunOnThreadPool(async () =>
            {
                await UniTask.DelayFrame(1);
                ReferencePool.Release(defaultValue);
            });
        }
        if (!Has(key))
        {
            return defaultValue;
        }
        var node = RootNode.GetChild(key);
        return node.GetData<T>();
    }

    /// <summary>
    /// 是否存在参数
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool Has(string key)
    {
        return RootNode.HasChild(key);
    }
    public void Clear()
    {
        GF.DataNode.RemoveNode(RootNode.Name);
    }
}
