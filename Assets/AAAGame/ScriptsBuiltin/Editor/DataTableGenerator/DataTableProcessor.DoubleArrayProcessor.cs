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
        private sealed class DoubleArrayProcessor : GenericDataProcessor<double[]>
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
                    return "double[]";
                }
            }

            public override int PopPriority => 90;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "double[]",
                    "system.double[]"
                };
            }

            public override double[] Parse(string value)
            {
                return DataTableExtension.ParseArray<double>(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                var v = Parse(value);
                binaryWriter.Write7BitEncodedInt32(v.Length);
                for (int i = 0; i < v.Length; i++)
                {
                    binaryWriter.Write(v[i]);
                }
            }
        }
    }
}
