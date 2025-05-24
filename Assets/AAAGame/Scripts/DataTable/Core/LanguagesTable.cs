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
/// LanguagesTable
/// </summary>
public class LanguagesTable : DataRowBase
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
        /// 多语言文件名
        /// </summary>
        public string LanguageKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 多语言资源名(相对路径)
        /// </summary>
        public string AssetName
        {
            get;
            private set;
        }

        /// <summary>
        /// 用于显示的语言名
        /// </summary>
        public string LanguageDisplay
        {
            get;
            private set;
        }

        /// <summary>
        /// 语言图标
        /// </summary>
        public string LanguageIcon
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
            LanguageKey = columnStrings[index++];
            AssetName = columnStrings[index++];
            LanguageDisplay = columnStrings[index++];
            LanguageIcon = columnStrings[index++];

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    LanguageKey = binaryReader.ReadString();
                    AssetName = binaryReader.ReadString();
                    LanguageDisplay = binaryReader.ReadString();
                    LanguageIcon = binaryReader.ReadString();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
