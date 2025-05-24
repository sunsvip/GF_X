﻿//------------------------------------------------------------
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
/// SoundGroup
/// </summary>
public class SoundGroupTable : DataRowBase
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
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int SoundAgentCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool AvoidBeingReplacedBySamePriority
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Mute
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public float Volume
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
            Name = columnStrings[index++];
            SoundAgentCount = int.Parse(columnStrings[index++]);
            AvoidBeingReplacedBySamePriority = bool.Parse(columnStrings[index++]);
            Mute = bool.Parse(columnStrings[index++]);
            Volume = float.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Name = binaryReader.ReadString();
                    SoundAgentCount = binaryReader.Read7BitEncodedInt32();
                    AvoidBeingReplacedBySamePriority = binaryReader.ReadBoolean();
                    Mute = binaryReader.ReadBoolean();
                    Volume = binaryReader.ReadSingle();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
