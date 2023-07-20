//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
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
        /// 
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
        /// 
        /// </summary>
        public bool PauseCoveredUI
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string UIGroup
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool EscapeClose
        {
            get;
            private set;
        }

        /// <summary>
        /// Custom,None,FadeIn,FadeOut,ScaleIn,ScaleOut
        /// </summary>
        public string OpenAnimType
        {
            get;
            private set;
        }

        /// <summary>
        /// Custom,None,FadeIn,FadeOut,ScaleIn,ScaleOut
        /// </summary>
        public string CloseAnimType
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
            UIGroup = columnStrings[index++];
            EscapeClose = bool.Parse(columnStrings[index++]);
            OpenAnimType = columnStrings[index++];
            CloseAnimType = columnStrings[index++];

            GeneratePropertyArray();
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
                    UIGroup = binaryReader.ReadString();
                    EscapeClose = binaryReader.ReadBoolean();
                    OpenAnimType = binaryReader.ReadString();
                    CloseAnimType = binaryReader.ReadString();
                }
            }

            GeneratePropertyArray();
            return true;
        }

        private void GeneratePropertyArray()
        {

        }
}
