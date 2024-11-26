//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace GameFramework.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class Int4ArrayProcessor : GenericDataProcessor<int4[]>
        {
            public override bool IsSystem
            {
                get
                {
                    return false;
                }
            }

            public override string LanguageKeyword
            {
                get
                {
                    return "int4[]";
                }
            }

            public override int PopPriority => 20;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "int4[]",
                    "Unity.Mathematics.int4[]"
                };
            }

            public override int4[] Parse(string value)
            {
                return DataTableExtension.Parseint4Array(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                var v = Parse(value);
                if (v == null)
                {
                    binaryWriter.Write7BitEncodedInt32(0);
                    return;
                }
                for (int i = 0; i < v.Length; i++)
                {
                    var itm = v[i];
                    binaryWriter.Write7BitEncodedInt32(itm.x);
                    binaryWriter.Write7BitEncodedInt32(itm.y);
                    binaryWriter.Write7BitEncodedInt32(itm.z);
                    binaryWriter.Write7BitEncodedInt32(itm.w);
                }
            }
        }
    }
}
