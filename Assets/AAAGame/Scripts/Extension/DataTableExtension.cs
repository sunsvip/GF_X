using GameFramework;
using GameFramework.DataTable;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityGameFramework.Runtime;
public static class DataTableExtension
{
    internal static readonly char[] DataSplitSeparators = new char[] { '\t' };
    internal static readonly char[] DataTrimSeparators = new char[] { '\"' };

    /// <summary>
    /// 加载数据表, 支持A/B测试
    /// </summary>
    /// <param name="dataTableComponent"></param>
    /// <param name="dataTableName"></param>
    /// <param name="abTestGroupName"></param>
    /// <param name="userData"></param>
    public static void LoadDataTable(this DataTableComponent dataTableComponent, string dataTableName, string abTestGroupName, object userData = null)
    {
        if (string.IsNullOrEmpty(dataTableName))
        {
            Log.Warning("Data table name is invalid.");
            return;
        }

        string[] splitNames = dataTableName.Split('_');
        if (splitNames.Length > 2)
        {
            Log.Warning("Data table name is invalid.");
            return;
        }

        string dataRowClassName = splitNames[0];

        Type dataRowType = Type.GetType(dataRowClassName);
        if (dataRowType == null)
        {
            Log.Warning("Can not get data row type with class name '{0}'.", dataRowClassName);
            return;
        }

        string name = splitNames.Length > 1 ? splitNames[1] : null;
        DataTableBase dataTable = dataTableComponent.CreateDataTable(dataRowType, name);

        string tableFileName = dataTableName;
        if (!string.IsNullOrWhiteSpace(abTestGroupName))
        {
            var abTableFileName = Utility.Text.Format("{0}{1}{2}", dataTableName, ConstBuiltin.AB_TEST_TAG, abTestGroupName);
            if (GFBuiltin.Resource.HasAsset(UtilityBuiltin.AssetsPath.GetDataTablePath(abTableFileName)) != GameFramework.Resource.HasAssetResult.NotExist)
            {
                tableFileName = abTableFileName;
            }
        }

        string assetName = UtilityBuiltin.AssetsPath.GetDataTablePath(tableFileName);
        dataTable.ReadData(assetName, userData);
    }

    /// <summary>
    /// 加载数据表
    /// 注意: 数据表类为热更新部分,所以需要从Hotfix程序集查找表类型
    /// </summary>
    /// <param name="dataTableComponent"></param>
    /// <param name="dataTableName"></param>
    /// <param name="userData"></param>
    public static void LoadDataTable(this DataTableComponent dataTableComponent, string dataTableName, object userData = null)
    {
        string abTestGroup = GFBuiltin.Setting.GetABTestGroup();
        dataTableComponent.LoadDataTable(dataTableName, abTestGroup, userData);
    }
    public static Color GetRandomColor(this DataTableComponent dataTableComponent)
    {
        var colorRows = GF.DataTable.GetDataTable<ColorTable>().GetAllDataRows();
        Color resultCol;

        int randomIdx = Utility.Random.GetRandom(0, colorRows.Length);

        if (ColorUtility.TryParseHtmlString(colorRows[randomIdx].ColorHex, out resultCol))
        {
            return resultCol;
        }
        else
        {
            resultCol = Color.white;
        }
        return resultCol;
    }


    public static Color32 ParseColor32(string value)
    {
        string[] splitValue = value.Split(',');
        return new Color32(byte.Parse(splitValue[0]), byte.Parse(splitValue[1]), byte.Parse(splitValue[2]), byte.Parse(splitValue[3]));
    }

    public static Color ParseColor(string value)
    {
        string[] splitValue = value.Split(',');
        return new Color(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }

    public static Quaternion ParseQuaternion(string value)
    {
        string[] splitValue = value.Split(',');
        return new Quaternion(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }

    public static Rect ParseRect(string value)
    {
        string[] splitValue = value.Split(',');
        return new Rect(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }

    public static Vector2 ParseVector2(string value)
    {
        string[] splitValue = value.Split(',');
        return new Vector2(float.Parse(splitValue[0]), float.Parse(splitValue[1]));
    }
    public static Vector2Int ParseVector2Int(string value)
    {
        string[] splitValue = value.Split(',');
        return new Vector2Int(int.Parse(splitValue[0]), int.Parse(splitValue[1]));
    }
    public static Vector3 ParseVector3(string value)
    {
        string[] splitValue = value.Split(',');

        return new Vector3(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]));
    }
    public static Vector3Int ParseVector3Int(string value)
    {
        string[] splitValue = value.Split(',');
        return new Vector3Int(int.Parse(splitValue[0]), int.Parse(splitValue[1]), int.Parse(splitValue[2]));
    }
    //public static LitJson.JsonData ParseJsonData(string value)
    //{
    //    return LitJson.JsonMapper.ToObject(value);
    //}
    //public static T ParseJsonData<T>(string value)
    //{
    //    return LitJson.JsonMapper.ToObject<T>(value);
    //}
    public static Vector4 ParseVector4(string value)
    {
        string[] splitValue = value.Split(',');
        return new Vector4(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }
    /// <summary>
    /// 解析数据表数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T[] ParseArray<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new T[0];
        }
        string[] strs = value.Split(',');
        T[] arr = new T[strs.Length];
        for (int i = 0; i < strs.Length; i++)
        {
            try
            {
                arr[i] = (T)Convert.ChangeType(strs[i], typeof(T));
            }
            catch (Exception e)
            {
                Log.Error("解析失败数据失败! 格式有误:{0}\nError:{1}", strs[i], e.Message);
            }
        }
        return arr;
    }
    /// <summary>
    /// 解析数据表2维数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T[][] Parse2DArray<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new T[0][];
        }
        var mats = Regex.Matches(value, "\\[.+?\\]");
        if (mats.Count > 0)
        {
            T[][] arr = new T[mats.Count][];
            for (int i = 0; i < mats.Count; i++)
            {
                string vstr = mats[i].Value;
                vstr = vstr.Substring(1, vstr.Length - 2);
                arr[i] = ParseArray<T>(vstr);
            }
            return arr;
        }
        return null;
    }
}