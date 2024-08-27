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
        private sealed class BoolArrayArrayProcessor : GenericDataProcessor<bool[][]>
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

            public override int PopPriority => 100;

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
                bool[][] arr = Parse(value);
                for (int i = 0; i < arr.Length; i++)
                {
                    for (int j = 0; j < arr[i].Length; j++)
                    {
                        binaryWriter.Write(arr[i][j]);
                    }
                }
            }
        }
    }
}
