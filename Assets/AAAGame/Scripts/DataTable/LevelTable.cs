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
/// 关卡表
/// </summary>
public class LevelTable : DataRowBase
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
        /// 关卡prefab名
        /// </summary>
        public string LvPfbName
        {
            get;
            private set;
        }

        /// <summary>
        /// 玩家初始钱数
        /// </summary>
        public int InitMoney
        {
            get;
            private set;
        }

        /// <summary>
        /// 取值1-6
        /// </summary>
        public int MoneyColorId
        {
            get;
            private set;
        }

        /// <summary>
        /// 关卡显示名(多语言)
        /// </summary>
        public string LvDisplayName
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
            LvPfbName = columnStrings[index++];
            InitMoney = int.Parse(columnStrings[index++]);
            MoneyColorId = int.Parse(columnStrings[index++]);
            LvDisplayName = columnStrings[index++];

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
                    LvPfbName = binaryReader.ReadString();
                    InitMoney = binaryReader.Read7BitEncodedInt32();
                    MoneyColorId = binaryReader.Read7BitEncodedInt32();
                    LvDisplayName = binaryReader.ReadString();
                }
            }

            GeneratePropertyArray();
            return true;
        }

        private void GeneratePropertyArray()
        {

        }
}
