
using GameFramework;

public abstract class DataModelStorageBase : DataModelBase
{
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
