using GameFramework;
using GameFramework.DataTable;
using System;
using System.Globalization;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;
public static class DataTableExtension
{
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    internal static readonly char[] DataSplitSeparators = new char[] { '\t' };
    internal static readonly char[] DataTrimSeparators = new char[] { '\"' };
    private static readonly NumberStyles IntegerNumberStyles = NumberStyles.Integer;
    private static readonly NumberStyles FloatingNumberStyles = NumberStyles.Float | NumberStyles.AllowThousands;

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

        Type dataRowType = Utility.Assembly.GetType(dataRowClassName);
        if (dataRowType == null)
        {
            Log.Warning("Can not get data row type with class name '{0}'.", dataRowClassName);
            return;
        }

        string name = splitNames.Length > 1 ? splitNames[1] : null;
        DataTableBase dataTable = dataTableComponent.CreateDataTable(dataRowType, name);

        dataTable.ReadData(GetDataTableAssetName(dataTableName, abTestGroupName, useBytes), userData);
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

    public static string GetDataTableAssetName(string dataTableName, string abTestGroupName, bool useBytes)
    {
        string tableFileName = dataTableName;
        if (!string.IsNullOrWhiteSpace(abTestGroupName))
        {
            var abTableFileName = Utility.Text.Format("{0}{1}{2}", dataTableName, ConstBuiltin.AB_TEST_TAG, abTestGroupName);
            if (GFBuiltin.Resource.HasAsset(UtilityBuiltin.AssetsPath.GetDataTablePath(abTableFileName, useBytes)) != GameFramework.Resource.HasAssetResult.NotExist)
            {
                tableFileName = abTableFileName;
            }
        }

        return UtilityBuiltin.AssetsPath.GetDataTablePath(tableFileName, useBytes);
    }

    public static string GetDataTableAssetName(string dataTableName, bool useBytes)
    {
        return GetDataTableAssetName(dataTableName, GFBuiltin.Setting.GetABTestGroup(), useBytes);
    }
    public static Color32 ParseColor32(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new Color32(255, 255, 255, 255);
        string[] splitValue = value.Split(',');
        return new Color32(ParseByte(splitValue[0]), ParseByte(splitValue[1]), ParseByte(splitValue[2]), ParseByte(splitValue[3]));
    }
    public static Color32 ReadColor32(this BinaryReader binaryReader)
    {
        return new Color32(binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte());
    }
    public static Color ParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Color.white;
        string[] splitValue = value.Split(',');
        return new Color(ParseFloat(splitValue[0]), ParseFloat(splitValue[1]), ParseFloat(splitValue[2]), ParseFloat(splitValue[3]));
    }
    public static Color ReadColor(this BinaryReader binaryReader)
    {
        return new Color(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static Quaternion ParseQuaternion(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Quaternion.identity;
        string[] splitValue = value.Split(',');
        return new Quaternion(ParseFloat(splitValue[0]), ParseFloat(splitValue[1]), ParseFloat(splitValue[2]), ParseFloat(splitValue[3]));
    }
    public static Quaternion ReadQuaternion(this BinaryReader binaryReader)
    {
        return new Quaternion(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static DateTime ParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;
        return DateTime.ParseExact(value, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
    public static DateTime ReadDateTime(this BinaryReader binaryReader)
    {
        return new DateTime(binaryReader.ReadInt64());
    }
    public static Rect ParseRect(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Rect.zero;
        string[] splitValue = value.Split(',');
        return new Rect(ParseFloat(splitValue[0]), ParseFloat(splitValue[1]), ParseFloat(splitValue[2]), ParseFloat(splitValue[3]));
    }
    public static Rect ReadRect(this BinaryReader binaryReader)
    {
        return new Rect(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
    }
    public static Vector2 ParseVector2(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Vector2.zero;
        string[] splitValue = value.Split(',');
        return new Vector2(ParseFloat(splitValue[0]), ParseFloat(splitValue[1]));
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
        if (length == -1) return null;
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
        return new Vector2Int(ParseInt(splitValue[0]), ParseInt(splitValue[1]));
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
        if (length == -1) return null;
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

        return new Vector3(ParseFloat(splitValue[0]), ParseFloat(splitValue[1]), ParseFloat(splitValue[2]));
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
        if (length == -1) return null;
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
        return new Vector3Int(ParseInt(splitValue[0]), ParseInt(splitValue[1]), ParseInt(splitValue[2]));
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
        if (length == -1) return null;
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
        return new Vector4(ParseFloat(splitValue[0]), ParseFloat(splitValue[1]), ParseFloat(splitValue[2]), ParseFloat(splitValue[3]));
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
        if (length == -1) return null;
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
        return new Unity.Mathematics.int4(ParseInt(splitValue[0]), ParseInt(splitValue[1]), ParseInt(splitValue[2]), ParseInt(splitValue[3]));
    }
    public static Unity.Mathematics.int4 Readint4(this BinaryReader binaryReader)
    {
        return new Unity.Mathematics.int4(binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32(), binaryReader.Read7BitEncodedInt32());
    }

    public static Unity.Mathematics.int4[] Parseint4Array(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string[] arr = ParseArrayElements(value);
        if (arr == null) return null;
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
        if (length == -1) return null;
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
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        Type enumType = typeof(TEnum);
        int raw = ParseEnumValue(enumType, enumType.Name, value);
        object enumValue = Enum.ToObject(enumType, raw);
        // 校验未定义的数值(如裸数字 "3" 对非 Flags 枚举): 告警并返回默认, 不让未定义值静默流入导致 switch 命中无 case。
        if (!Enum.IsDefined(enumType, enumValue) && !IsDefinedFlagsEnumValue(enumType, raw))
        {
            Log.Warning("Enum {0} value '{1}' (raw={2}) is not defined, use default.", enumType.Name, value, raw);
            return default;
        }

        return (TEnum)enumValue;
    }
    public static TEnum ReadEnum<TEnum>(this BinaryReader binaryReader) where TEnum : struct, Enum
    {
        int value = binaryReader.Read7BitEncodedInt32();
        return ToEnum<TEnum>(value);
    }

    public static T ParseJson<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "null", StringComparison.OrdinalIgnoreCase))
        {
            return default;
        }

        return Utility.Json.ToObject<T>(value);
    }

    public static T ReadJson<T>(this BinaryReader binaryReader)
    {
        return ParseJson<T>(binaryReader.ReadString());
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
        Type type = typeof(T);

        if (type == typeof(int))
        {
            int[] result = new int[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseInt(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(float))
        {
            float[] result = new float[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseFloat(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(double))
        {
            double[] result = new double[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseDouble(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(long))
        {
            long[] result = new long[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseLong(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(uint))
        {
            uint[] result = new uint[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseUInt(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(short))
        {
            short[] result = new short[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseShort(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(ushort))
        {
            ushort[] result = new ushort[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseUShort(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(ulong))
        {
            ulong[] result = new ulong[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseULong(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(byte))
        {
            byte[] result = new byte[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseByte(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(sbyte))
        {
            sbyte[] result = new sbyte[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseSByte(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(bool))
        {
            bool[] result = new bool[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseBool(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(char))
        {
            char[] result = new char[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseChar(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(decimal))
        {
            decimal[] result = new decimal[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseDecimal(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(DateTime))
        {
            DateTime[] result = new DateTime[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    result[i] = ParseDateTime(strs[i]);
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return (T[])(object)result;
        }

        if (type == typeof(string))
        {
            return (T[])(object)strs;
        }

        if (type.IsEnum)
        {
            // 文本路径保持宽松: ParseEnumValue 已对未知成员告警返回 0, Enum.ToObject 不抛异常,
            // 单元格异常仍被捕获, 避免一个坏单元格中断整行/整表加载。
            T[] arr = new T[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                try
                {
                    arr[i] = (T)Enum.ToObject(type, ParseEnumValue(type, type.Name, strs[i]));
                }
                catch (Exception e)
                {
                    LogParseArrayFailure(strs[i], e);
                }
            }
            return arr;
        }

        throw new GameFrameworkException(Utility.Text.Format("Can not parse array for unsupported type '{0}'.", type.FullName));
    }

    private static void LogParseArrayFailure(string element, Exception e)
    {
        Log.Error("解析数组元素失败! 格式有误:{0}\nError:{1}", element, e.Message);
    }
    public static T[] ReadArray<T>(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        if (length == -1) return null;

        Type type = typeof(T);
        if (type == typeof(int))
        {
            int[] result = new int[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.Read7BitEncodedInt32();
            }
            return (T[])(object)result;
        }

        if (type == typeof(float))
        {
            float[] result = new float[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.ReadSingle();
            }
            return (T[])(object)result;
        }

        if (type == typeof(double))
        {
            double[] result = new double[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.ReadDouble();
            }
            return (T[])(object)result;
        }

        if (type == typeof(long))
        {
            long[] result = new long[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.Read7BitEncodedInt64();
            }
            return (T[])(object)result;
        }

        if (type == typeof(bool))
        {
            bool[] result = new bool[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.ReadBoolean();
            }
            return (T[])(object)result;
        }

        if (type == typeof(string))
        {
            string[] result = new string[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.ReadString();
            }
            return (T[])(object)result;
        }

        if (type == typeof(byte))
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.ReadByte();
            }
            return (T[])(object)result;
        }

        if (type == typeof(char))
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = binaryReader.ReadChar();
            }
            return (T[])(object)result;
        }

        if (type.IsEnum)
        {
            T[] arr = new T[length];
            for (int i = 0; i < length; i++)
            {
                int value = binaryReader.Read7BitEncodedInt32();
                arr[i] = (T)ToEnum(type, value);
            }
            return arr;
        }

        if (type == typeof(DateTime))
        {
            DateTime[] result = new DateTime[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = new DateTime(binaryReader.ReadInt64());
            }
            return (T[])(object)result;
        }

        throw new GameFrameworkException(Utility.Text.Format("Can not read array for unsupported type '{0}'.", type.FullName));
    }
    /// <summary>
    /// 解析数据表2维数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T[][] Parse2DArray<T>(string value)
    {
        string[] elements = ParseArrayElements(value);
        if (elements == null)
        {
            return null;
        }

        T[][] arr = new T[elements.Length][];
        for (int i = 0; i < elements.Length; i++)
        {
            arr[i] = ParseArray<T>(elements[i]);
        }

        return arr;
    }
    public static T[][] Read2DArray<T>(this BinaryReader binaryReader)
    {
        int length = binaryReader.Read7BitEncodedInt32();
        if (length == -1) return null;
        T[][] arr = new T[length][];
        for (int i = 0; i < length; i++)
        {
            arr[i] = ReadArray<T>(binaryReader);
        }
        return arr;
    }

    public static Type ParseType(string value)
    {
        return Utility.Assembly.GetType(value);
    }
    public static Type ReadType(this BinaryReader binaryReader)
    {
        return ParseType(binaryReader.ReadString());
    }
    private static string[] ParseArrayElements(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        int start = -1;
        int count = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '[')
            {
                start = i + 1;
            }
            else if (c == ']' && start >= 0)
            {
                // 仅当括号内非空时计入元素: '[]' 视为 0 元素 (与基线 Regex [.+?] 一致, 最终返回 null),
                // 避免 '[]' 被误判为 1 个空元素导致下游解析出 NullReferenceException/越界。
                if (i > start)
                {
                    count++;
                }
                start = -1;
            }
        }

        if (count == 0)
        {
            return null;
        }

        string[] elements = new string[count];
        start = -1;
        int elementIndex = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '[')
            {
                start = i + 1;
            }
            else if (c == ']' && start >= 0)
            {
                if (i > start)
                {
                    elements[elementIndex++] = value.Substring(start, i - start);
                }
                start = -1;
            }
        }

        return elements;
    }
    public static bool TryParseEnum(string enumValue, out Type enumType, out int value)
    {
        enumType = null;
        value = 0;

        if (string.IsNullOrWhiteSpace(enumValue))
        {
            return false;
        }

        int dotIndex = enumValue.IndexOf('.');
        if (dotIndex <= 0)
        {
            return false;
        }

        string enumName = enumValue.Substring(0, dotIndex);

        enumType = Utility.Assembly.GetType(enumName);
        if (enumType == null)
        {
            Type[] types = Utility.Assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type.IsEnum && type.Name == enumName)
                {
                    enumType = type;
                    break;
                }
            }
        }

        if (enumType == null || !enumType.IsEnum)
        {
            return false;
        }

        try
        {
            value = ParseEnumValue(enumType, enumName, enumValue);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static bool TryParseEnum(string enumValue, out Type enumType)
    {
        return TryParseEnum(enumValue, out enumType, out _);
    }

    public static bool ParseBool(string value)
    {
        value = value.Trim();
        if (value == "1")
        {
            return true;
        }

        if (value == "0")
        {
            return false;
        }

        return bool.Parse(value);
    }

    public static byte ParseByte(string value)
    {
        return byte.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static sbyte ParseSByte(string value)
    {
        return sbyte.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static short ParseShort(string value)
    {
        return short.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static ushort ParseUShort(string value)
    {
        return ushort.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static int ParseInt(string value)
    {
        return int.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static uint ParseUInt(string value)
    {
        return uint.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static long ParseLong(string value)
    {
        return long.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static ulong ParseULong(string value)
    {
        return ulong.Parse(value, IntegerNumberStyles, CultureInfo.InvariantCulture);
    }

    public static char ParseChar(string value)
    {
        return char.Parse(value);
    }

    public static float ParseFloat(string value)
    {
        return float.Parse(value, FloatingNumberStyles, CultureInfo.InvariantCulture);
    }

    public static double ParseDouble(string value)
    {
        return double.Parse(value, FloatingNumberStyles, CultureInfo.InvariantCulture);
    }

    public static decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, FloatingNumberStyles, CultureInfo.InvariantCulture);
    }

    private static int ParseEnumValue(Type enumType, string enumName, string value)
    {
        int result = 0;
        bool hasFlags = false;
        int startIndex = 0;
        while (startIndex < value.Length)
        {
            int endIndex = value.IndexOf('|', startIndex);
            if (endIndex < 0)
            {
                endIndex = value.Length;
            }
            else
            {
                hasFlags = true;
            }

            int dotIndex = value.IndexOf('.', startIndex, endIndex - startIndex);
            // 要求限定形式 "EnumName.Member" (与基线一致); 裸成员名会掩盖前缀拼写错误, 不接受。
            if (dotIndex <= startIndex || dotIndex + 1 >= endIndex)
            {
                Log.Warning("Value '{0}' is not defined in enum {1}, use default.", value, enumName);
                return 0;
            }

            int enumNameLength = dotIndex - startIndex;
            if (enumNameLength != enumName.Length ||
                string.Compare(value, startIndex, enumName, 0, enumName.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                Log.Warning("Value '{0}' is not defined in enum {1}, use default.", value, enumName);
                return 0;
            }

            string member = value.Substring(dotIndex + 1, endIndex - dotIndex - 1);

            // 大小写不敏感解析(与基线 flags 行为一致), 未知成员告警并返回默认, 不抛异常以免中断整表加载
            if (Enum.TryParse(enumType, member, true, out object memberValue))
            {
                result |= Convert.ToInt32(memberValue);
            }
            else
            {
                Log.Warning("Value '{0}' is not defined in enum {1}, use default.", value, enumName);
                return 0;
            }

            startIndex = endIndex + 1;
        }

        // 非 Flags 枚举却使用 '|' 组合: 仅告警并返回默认, 不抛出 (基线在此处宽松 OR 后返回)
        if (hasFlags && !enumType.IsDefined(typeof(FlagsAttribute), false))
        {
            Log.Warning("Value '{0}' uses '|' combination but enum {1} is not Flags, use default.", value, enumName);
            return 0;
        }

        return result;
    }

    private static TEnum ToEnum<TEnum>(int value) where TEnum : struct, Enum
    {
        return (TEnum)ToEnum(typeof(TEnum), value);
    }

    private static object ToEnum(Type enumType, int value)
    {
        object enumValue = Enum.ToObject(enumType, value);
        if (Enum.IsDefined(enumType, enumValue) || IsDefinedFlagsEnumValue(enumType, value))
        {
            return enumValue;
        }

        throw new GameFrameworkException(Utility.Text.Format("Value {0} is not defined in enum {1}.", value, enumType.Name));
    }

    private static bool IsDefinedFlagsEnumValue(Type enumType, int value)
    {
        if (!enumType.IsDefined(typeof(FlagsAttribute), false))
        {
            return false;
        }

        int definedMask = 0;
        Array values = Enum.GetValues(enumType);
        for (int i = 0; i < values.Length; i++)
        {
            definedMask |= Convert.ToInt32(values.GetValue(i));
        }

        return (value & ~definedMask) == 0;
    }
}
