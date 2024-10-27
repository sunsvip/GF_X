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
/// CombatUnitTable
/// </summary>
public class CombatUnitTable : DataRowBase
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
        /// 预制体资源
        /// </summary>
        public string PrefabName
        {
            get;
            private set;
        }

        /// <summary>
        /// 攻击半径
        /// </summary>
        public float AttackRadius
        {
            get;
            private set;
        }

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 默认血量
        /// </summary>
        public int Hp
        {
            get;
            private set;
        }

        /// <summary>
        /// 伤害
        /// </summary>
        public int Damage
        {
            get;
            private set;
        }

        /// <summary>
        /// 单次攻击敌人个数
        /// </summary>
        public int MaxAttackCount
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
            PrefabName = columnStrings[index++];
            AttackRadius = float.Parse(columnStrings[index++]);
            MoveSpeed = float.Parse(columnStrings[index++]);
            Hp = int.Parse(columnStrings[index++]);
            Damage = int.Parse(columnStrings[index++]);
            MaxAttackCount = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    PrefabName = binaryReader.ReadString();
                    AttackRadius = binaryReader.ReadSingle();
                    MoveSpeed = binaryReader.ReadSingle();
                    Hp = binaryReader.Read7BitEncodedInt32();
                    Damage = binaryReader.Read7BitEncodedInt32();
                    MaxAttackCount = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
