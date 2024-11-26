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
        private sealed class Vector4ArrayProcessor : GenericDataProcessor<Vector4[]>
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
                    return "Vector4[]";
                }
            }

            public override int PopPriority => 20;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "vector4[]",
                    "unityengine.vector4[]]"
                };
            }

            public override Vector4[] Parse(string value)
            {
                return DataTableExtension.ParseVector4Array(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                var v = Parse(value);
                if(v == null)
                {
                    binaryWriter.Write7BitEncodedInt32(0);
                    return;
                }
                binaryWriter.Write7BitEncodedInt32(v.Length);
                for (int i = 0; i < v.Length; i++)
                {
                    var itm = v[i];
                    binaryWriter.Write(itm.x);
                    binaryWriter.Write(itm.y);
                    binaryWriter.Write(itm.z);
                    binaryWriter.Write(itm.w);
                }
            }
        }
    }
}
