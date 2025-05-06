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
        private sealed class Vector3IntArrayProcessor : GenericDataProcessor<Vector3Int[]>
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
                    return "Vector3Int[]";
                }
            }

            public override int ShowOrder => 20;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "vector3int[]",
                    "unityengine.vector3int[]"
                };
            }

            public override Vector3Int[] Parse(string value)
            {
                return DataTableExtension.ParseVector3IntArray(value);
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
                    binaryWriter.Write7BitEncodedInt32(itm.z);
                }
            }
        }
    }
}
