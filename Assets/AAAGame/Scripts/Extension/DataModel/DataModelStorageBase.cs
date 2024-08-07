
using GameFramework;
using UnityGameFramework.Runtime;

public class DataModelStorageBase : DataModelBase
{
    public int Hp { get; private set; } = 99;
    public float HpSpeed { get; private set; } = 12;
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
