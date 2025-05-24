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

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
/// <summary>
/// UI界面表
/// </summary>
public class UITable : DataRowBase
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
        /// 显示顺序,相对于Group,每个Group间隔100
        /// </summary>
        public int SortOrder
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string UIPrefab
        {
            get;
            private set;
        }

        /// <summary>
        /// 同组界面被覆盖时是否隐藏
        /// </summary>
        public bool PauseCoveredUI
        {
            get;
            private set;
        }

        /// <summary>
        /// UI组Id
        /// </summary>
        public int UIGroupId
        {
            get;
            private set;
        }

        /// <summary>
        /// 返回键触发关闭界面
        /// </summary>
        public bool EscapeClose
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
            SortOrder = int.Parse(columnStrings[index++]);
            UIPrefab = columnStrings[index++];
            PauseCoveredUI = bool.Parse(columnStrings[index++]);
            UIGroupId = int.Parse(columnStrings[index++]);
            EscapeClose = bool.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    SortOrder = binaryReader.Read7BitEncodedInt32();
                    UIPrefab = binaryReader.ReadString();
                    PauseCoveredUI = binaryReader.ReadBoolean();
                    UIGroupId = binaryReader.Read7BitEncodedInt32();
                    EscapeClose = binaryReader.ReadBoolean();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
