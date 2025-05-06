﻿using GameFramework;
using GameFramework.DataTable;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Mathematics;
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
    public static void LoadDataTable(this DataTableComponent dataTableComponent, string dataTableName, string abTestGroupName, bool useBytes, object userData = null)
    {
        if (string.IsNullOrWhiteSpace(dataTableName))
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

        string dataRowClassName = System.IO.Path.GetFileName(splitNames[0]);

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
            if (GFBuiltin.Resource.HasAsset(UtilityBuiltin.AssetsPath.GetDataTablePath(abTableFileName, useBytes)) != GameFramework.Resource.HasAssetResult.NotExist)
            {
                tableFileName = abTableFileName;
            }
        }

        string assetName = UtilityBuiltin.AssetsPath.GetDataTablePath(tableFileName, useBytes);
        dataTable.ReadData(assetName, userData);
    }

    /// <summary>
    /// 加载数据表
    /// 注意: 数据表类为热更新部分,所以需要从Hotfix程序集查找表类型
    /// </summary>
    /// <param name="dataTableComponent"></param>
    /// <param name="dataTableName"></param>
    /// <param name="userData"></param>
    public static void LoadDataTable(this DataTableComponent dataTableComponent, string dataTableName, bool useBytes, object userData = null)
    {
        string abTestGroup = GFBuiltin.Setting.GetABTestGroup();
        dataTableComponent.LoadDataTable(dataTableName, abTestGroup, useBytes, userData);
    }
    public static Color32 ParseColor32(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new Color32(255, 255, 255, 255);
        string[] splitValue = value.Split(',');
        return new Color32(byte.Parse(splitValue[0]), byte.Parse(splitValue[1]), byte.Parse(splitValue[2]), byte.Parse(splitValue[3]));
    }
    public static Color32 ReadColor32(this BinaryReader binaryReader)
    {
        return new Color32(binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte());
    }
    public static Color ParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Color.white;
        string[] splitValue = value.Split(',');
        return new Color(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }
    public static Color ReadColor(this BinaryReader binaryReader)
    {
        return new Color(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static Quaternion ParseQuaternion(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Quaternion.identity;
        string[] splitValue = value.Split(',');
        return new Quaternion(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }
    public static Quaternion ReadQuaternion(this BinaryReader binaryReader)
    {
        return new Quaternion(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static DateTime ParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;
        return DateTime.Parse(value);
    }
    public static DateTime ReadDateTime(this BinaryReader binaryReader)
    {
        return new DateTime(binaryReader.ReadInt64());
    }
    public static Rect ParseRect(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Rect.zero;
        string[] splitValue = value.Split(',');
        return new Rect(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }
    public static Rect ReadRect(this BinaryReader binaryReader)
    {
        return new Rect(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static Vector2 ParseVector2(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Vector2.zero;
        string[] splitValue = value.Split(',');
        return new Vector2(float.Parse(splitValue[0]), float.Parse(splitValue[1]));
    }
    public static Vector2 ReadVector2(this BinaryReader binaryReader)
    {
        return new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }

    public static Vector2[] ParseVector2Array(string value)
    {
        string[] arr = ParseArrayElements(value);
        if (arr == null) return null;
        Vector2[] result = new Vector2[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            result[i] = ParseVector2(arr[i]);
        }
        return result;
    }
    public static Vector2[] ReadVector2Array(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        Vector2[] result = new Vector2[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = binaryReader.ReadVector2();
        }
        return result;
    }
    public static Vector2Int ParseVector2Int(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Vector2Int.zero;
        string[] splitValue = value.Split(',');
        return new Vector2Int(int.Parse(splitValue[0]), int.Parse(splitValue[1]));
    }
    public static Vector2Int ReadVector2Int(this BinaryReader binaryReader)
    {
        return new Vector2Int(binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32());
    }
    public static Vector2Int[] ParseVector2IntArray(string value)
    {
        string[] arr = ParseArrayElements(value);
        if (arr == null) return null;
        Vector2Int[] result = new Vector2Int[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            result[i] = ParseVector2Int(arr[i]);
        }
        return result;
    }
    public static Vector2Int[] ReadVector2IntArray(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        Vector2Int[] result = new Vector2Int[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = binaryReader.ReadVector2Int();
        }
        return result;
    }
    public static Vector3 ParseVector3(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Vector3.zero;
        string[] splitValue = value.Split(',');

        return new Vector3(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]));
    }
    public static Vector3 ReadVector3(this BinaryReader binaryReader)
    {
        return new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static Vector3[] ParseVector3Array(string value)
    {
        string[] arr = ParseArrayElements(value);
        if (arr == null) return null;
        Vector3[] result = new Vector3[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            result[i] = ParseVector3(arr[i]);
        }
        return result;
    }
    public static Vector3[] ReadVector3Array(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        Vector3[] result = new Vector3[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = binaryReader.ReadVector3();
        }
        return result;
    }
    public static Vector3Int ParseVector3Int(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Vector3Int.zero;
        string[] splitValue = value.Split(',');
        return new Vector3Int(int.Parse(splitValue[0]), int.Parse(splitValue[1]), int.Parse(splitValue[2]));
    }
    public static Vector3Int ReadVector3Int(this BinaryReader binaryReader)
    {
        return new Vector3Int(binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32());
    }
    public static Vector3Int[] ParseVector3IntArray(string value)
    {
        string[] arr = ParseArrayElements(value);
        if (arr == null) return null;
        Vector3Int[] result = new Vector3Int[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            result[i] = ParseVector3Int(arr[i]);
        }
        return result;
    }
    public static Vector3Int[] ReadVector3IntArray(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        Vector3Int[] result = new Vector3Int[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = binaryReader.ReadVector3Int();
        }
        return result;
    }
    public static Vector4 ParseVector4(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Vector4.zero;
        string[] splitValue = value.Split(',');
        return new Vector4(float.Parse(splitValue[0]), float.Parse(splitValue[1]), float.Parse(splitValue[2]), float.Parse(splitValue[3]));
    }
    public static Vector4 ReadVector4(this BinaryReader binaryReader)
    {
        return new Vector4(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static Vector4[] ParseVector4Array(string value)
    {
        string[] arr = ParseArrayElements(value);
        if (arr == null) return null;

        Vector4[] result = new Vector4[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            result[i] = ParseVector4(arr[i]);
        }
        return result;
    }
    public static Vector4[] ReadVector4Array(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        Vector4[] result = new Vector4[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = binaryReader.ReadVector4();
        }
        return result;
    }
    public static Unity.Mathematics.int4 Parseint4(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return int4.zero;
        string[] splitValue = value.Split(',');
        return new Unity.Mathematics.int4(int.Parse(splitValue[0]), int.Parse(splitValue[1]), int.Parse(splitValue[2]), int.Parse(splitValue[3]));
    }
    public static Unity.Mathematics.int4 Readint4(this BinaryReader binaryReader)
    {
        return new Unity.Mathematics.int4(binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32());
    }

    public static Unity.Mathematics.int4[] Parseint4Array(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string[] arr = ParseArrayElements(value);
        Unity.Mathematics.int4[] result = new Unity.Mathematics.int4[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            result[i] = Parseint4(arr[i]);
        }
        return result;
    }
    public static Unity.Mathematics.int4[] Readint4Array(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        Unity.Mathematics.int4[] result = new Unity.Mathematics.int4[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = binaryReader.Readint4();
        }
        return result;
    }
    /// <summary>
    /// 解析枚举
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        if (!string.IsNullOrEmpty(value))
        {
            string[] splitValue = value.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (splitValue.Length == 1)
            {
                var valueStr = splitValue[0].Split('.')[1];
                if (Enum.TryParse<TEnum>(valueStr, out TEnum result))
                {
                    return result;
                }
            }
            else
            {
                int resultEnum = 0;
                foreach (string s in splitValue)
                {
                    var strTrim = s.Split('.')[1].Trim();
                    if (Enum.TryParse<TEnum>(strTrim, true, out TEnum result))
                    {
                        resultEnum |= Convert.ToInt32(result);
                    }
                }
                return (TEnum)Enum.ToObject(typeof(TEnum), resultEnum);
            }
        }
        return default(TEnum);
    }
    public static TEnum ReadEnum<TEnum>(this BinaryReader binaryReader) where TEnum : struct, Enum
    {
        int value = binaryReader.Read7BitEncodedInt32();
        if (Enum.IsDefined(typeof(TEnum), value))
        {
            return (TEnum)(object)value;
        }
        throw new GameFrameworkException(Utility.Text.Format("Value {0} is not defined in enum {1}.", value, typeof(TEnum).Name));
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
            return null;
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
    public static T[] ReadArray<T>(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        T[] arr = new T[length];
        Type type = typeof(T);
        if (type == typeof(int))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.Read7BitEncodedInt32();
            }
        }
        else if (type == typeof(float))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.ReadSingle();
            }
        }
        else if (type == typeof(double))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.Read();
            }
        }
        else if (type == typeof(long))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.Read();
            }
        }
        else if (type == typeof(bool))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.ReadBoolean();
            }
        }
        else if (type == typeof(string))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.ReadString();
            }
        }
        else if (type == typeof(byte))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.ReadByte();
            }
        }
        else if (type == typeof(char))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)binaryReader.ReadChar();
            }
        }
        else if (type.IsEnum)
        {
            for (int i = 0; i < length; i++)
            {
                int value = binaryReader.Read7BitEncodedInt32();
                if (Enum.IsDefined(type, value))
                {
                    arr[i] = (T)(object)value;
                }
                else
                {
                    throw new GameFrameworkException(Utility.Text.Format("Value {0} is not defined in enum {1}.", value, type.Name));
                }
            }
        }
        else if (type == typeof(DateTime))
        {
            for (int i = 0; i < length; i++)
            {
                arr[i] = (T)(object)new DateTime(binaryReader.ReadInt64());
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
            return null;
        }
        var mats = Regex.Matches(value, "\\[.+?\\]");
        if (mats.Count > 0)
        {
            T[][] arr = new T[mats.Count][];
            for (int i = 0; i < mats.Count; i++)
            {
                string vstr = mats[i].Value;
                vstr = vstr[1..^1];
                arr[i] = ParseArray<T>(vstr);
            }
            return arr;
        }
        return null;
    }
    public static T[][] Read2DArray<T>(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        T[][] arr = new T[length][];
        for (int i = 0; i < length; i++)
        {
            arr[i] = ReadArray<T>(binaryReader);
        }
        return arr;
    }

    public static Type ParseType(string value)
    {
        return Type.GetType(value);
    }
    public static Type ReadType(this BinaryReader binaryReader)
    {
        return ParseType(binaryReader.ReadString());
    }
    private static string[] ParseArrayElements(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var mats = Regex.Matches(value, "\\[.+?\\]");
        if (mats.Count > 0)
        {
            string[] arr = new string[mats.Count];
            for (int i = 0; i < mats.Count; i++)
            {
                string vstr = mats[i].Value;
                arr[i] = vstr[1..^1];
            }
            return arr;
        }
        return null;
    }
    public static bool TryParseEnum(string enumValue, out Type enumType, out int value)
    {
        enumType = null;
        value = 0;
        var enumElements = enumValue.Split('.');
        if (enumElements.Length != 2)
        {
            return false;
        }
        var enumName = enumElements[0];
        enumType = Utility.Assembly.GetType(enumName);
        if (enumType == null)
        {
            enumType = Utility.Assembly.GetTypes().FirstOrDefault(t => t.IsEnum && (t.Name == enumName));
        }
        if (enumType != null)
        {
            value = (int)Enum.Parse(enumType, enumElements[1]);
        }
        return enumType != null && enumType.IsEnum;
    }
    public static bool TryParseEnum(string enumValue, out Type enumType)
    {
        return TryParseEnum(enumValue, out enumType, out _);
    }
}