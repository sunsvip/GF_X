//------------------------------------------------------------
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
// 生成时间：__DATA_TABLE_CREATE_TIME__
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

[System.Reflection.Obfuscation(Feature = "renaming", ApplyToMembers = false)]
/// <summary>
/// TestTable
/// </summary>
public class TestTable : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 请添加字段, 字段名首字母大写
        /// </summary>
        public string[] StringArr
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public CombatUnitEntity.CombatFlag EnumValue
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Vec3
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3Int Vec3Int
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector2 Vec2
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector2[] Vec2Arr
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector4[] Vec4Arr
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3[] Vec3Arr
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool[] BoolArr
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public float[][] Float2DArr
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool[][] Bool2DArr
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool BoolValue
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime DateTimeValue
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            StringArr = DataTableExtension.ParseArray<string>(columnStrings[index++]);
            EnumValue = DataTableExtension.ParseEnum<CombatUnitEntity.CombatFlag>(columnStrings[index++]);
            Vec3 = DataTableExtension.ParseVector3(columnStrings[index++]);
            Vec3Int = DataTableExtension.ParseVector3Int(columnStrings[index++]);
            Vec2 = DataTableExtension.ParseVector2(columnStrings[index++]);
            Vec2Arr = DataTableExtension.ParseVector2Array(columnStrings[index++]);
            Vec4Arr = DataTableExtension.ParseVector4Array(columnStrings[index++]);
            Vec3Arr = DataTableExtension.ParseVector3Array(columnStrings[index++]);
            BoolArr = DataTableExtension.ParseArray<bool>(columnStrings[index++]);
            Float2DArr = DataTableExtension.Parse2DArray<float>(columnStrings[index++]);
            Bool2DArr = DataTableExtension.Parse2DArray<bool>(columnStrings[index++]);
            BoolValue = bool.Parse(columnStrings[index++]);
            DateTimeValue = DateTime.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    StringArr = binaryReader.ReadArray<string>();
                    EnumValue = binaryReader.ReadEnum<CombatUnitEntity.CombatFlag>();
                    Vec3 = binaryReader.ReadVector3();
                    Vec3Int = binaryReader.ReadVector3Int();
                    Vec2 = binaryReader.ReadVector2();
                    Vec2Arr = binaryReader.ReadVector2Array();
                    Vec4Arr = binaryReader.ReadVector4Array();
                    Vec3Arr = binaryReader.ReadVector3Array();
                    BoolArr = binaryReader.ReadArray<bool>();
                    Float2DArr = binaryReader.Read2DArray<float>();
                    Bool2DArr = binaryReader.Read2DArray<bool>();
                    BoolValue = binaryReader.ReadBoolean();
                    DateTimeValue = binaryReader.ReadDateTime();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
