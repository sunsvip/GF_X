
using GameFramework;
using UnityGameFramework.Runtime;

public abstract class DataModelStorageBase : DataModelBase
{
    protected override void OnCreate(RefParams userdata)
    {
        base.OnCreate(userdata);
        InitStorageData();
    }

    protected override void OnRelease()
    {
        Save();
    }

    private void InitStorageData()
    {
        string dataJson = GF.Setting.GetString(this.GetType().FullName);
        if (!string.IsNullOrEmpty(dataJson))
        {
            Newtonsoft.Json.JsonConvert.PopulateObject(dataJson, this);
        }
    }
    public void Save()
    {
        string dataJson = Utility.Json.ToJson(this);
        if (!string.IsNullOrEmpty(dataJson))
        {
            GF.Setting.SetString(this.GetType().FullName, dataJson);
        }
    }
}
