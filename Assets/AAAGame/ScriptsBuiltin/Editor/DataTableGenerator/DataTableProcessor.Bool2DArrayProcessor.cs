﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.IO;
using UnityEngine;

namespace GameFramework.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class Bool2DArrayProcessor : GenericDataProcessor<bool[][]>
        {
            public override bool IsSystem
            {
                get
                {
                    return true;
                }
            }

            public override string LanguageKeyword
            {
                get
                {
                    return "bool[][]";
                }
            }

            public override int ShowOrder => 100;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "bool[][]",
                    "system.bool[][]"
                };
            }

            public override bool[][] Parse(string value)
            {
                return DataTableExtension.Parse2DArray<bool>(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                var v = Parse(value);
                if (v == null)
                {
                    binaryWriter.Write7BitEncodedInt32(0);
                    return;
                }
                binaryWriter.Write7BitEncodedInt32(v.Length);
                for (int i = 0; i < v.Length; i++)
                {
                    var itm = v[i];
                    if (itm == null)
                    {
                        binaryWriter.Write7BitEncodedInt32(0);
                        continue;
                    }
                    binaryWriter.Write7BitEncodedInt32(itm.Length);
                    for (int j = 0; j < itm.Length; j++)
                    {
                        binaryWriter.Write(itm[j]);
                    }
                }
            }
        }
    }
}
