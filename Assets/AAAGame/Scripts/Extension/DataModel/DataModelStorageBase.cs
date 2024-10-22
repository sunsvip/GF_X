
using GameFramework;
using UnityGameFramework.Runtime;
/// <summary>
/// 数据模型, 可持久化保存
/// </summary>
public abstract class DataModelStorageBase : DataModelBase
{
    protected string StorageKey { get; private set; } = null;
    public DataModelStorageBase()
    {
        StorageKey = this.GetType().FullName;
    }
    protected override void OnCreate(RefParams userdata)
    {
        base.OnCreate(userdata);
        Load();
    }

    protected override void OnRelease()
    {
        Save();
    }

    private void Load()
    {
        if (Id != 0)
        {
            OnInitialDataModel();
            return;
        }
        string dataJson = GF.Setting.GetString(StorageKey, null);
        if (!string.IsNullOrEmpty(dataJson))
        {
            Newtonsoft.Json.JsonConvert.PopulateObject(dataJson, this);
        }
        else
        {
            OnInitialDataModel();
        }
    }
    /// <summary>
    /// 从没有本地储存数据时, 回调此方法, 用于初始化变量
    /// </summary>
    protected virtual void OnInitialDataModel() { }

    public void Save()
    {
        if (Id != 0) return;
        string dataJson = Utility.Json.ToJson(this);
        if (!string.IsNullOrEmpty(dataJson))
        {
            GF.Setting.SetString(StorageKey, dataJson);
        }
    }
}
