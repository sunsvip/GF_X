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
        private sealed class IntArrayProcessor : GenericDataProcessor<int[]>
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
                    return "int[]";
                }
            }

            public override int PopPriority => 90;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "int[]",
                    "int32[]",
                    "system.int32[]"
                };
            }

            public override int[] Parse(string value)
            {
                return DataTableExtension.ParseArray<int>(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                int[] arr = Parse(value);
                for (int i = 0; i < arr.Length; i++)
                {
                    binaryWriter.Write(arr[i]);
                }
            }
        }
    }
}
