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
        private sealed class Int4Processor : GenericDataProcessor<int4>
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
                    return "int4";
                }
            }

            public override int PopPriority => 20;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "int4",
                    "Unity.Mathematics.int4"
                };
            }

            public override int4 Parse(string value)
            {
                return DataTableExtension.Parseint4(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                int4 vector3 = Parse(value);
                binaryWriter.Write7BitEncodedInt32(vector3.x);
                binaryWriter.Write7BitEncodedInt32(vector3.y);
                binaryWriter.Write7BitEncodedInt32(vector3.z);
                binaryWriter.Write7BitEncodedInt32(vector3.w);
            }
        }
    }
}
