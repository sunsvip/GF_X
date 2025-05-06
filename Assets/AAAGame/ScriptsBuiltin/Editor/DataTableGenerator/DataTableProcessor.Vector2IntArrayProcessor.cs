//------------------------------------------------------------
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
        private sealed class Vector2IntArrayProcessor : GenericDataProcessor<Vector2Int[]>
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
                    return "Vector2Int[]";
                }
            }

            public override int ShowOrder => 29;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "vector2int[]",
                    "unityengine.vector2int[]"
                };
            }

            public override Vector2Int[] Parse(string value)
            {
                return DataTableExtension.ParseVector2IntArray(value);
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
                    binaryWriter.Write7BitEncodedInt32(itm.x);
                    binaryWriter.Write7BitEncodedInt32(itm.y);
                }
            }
        }
    }
}
