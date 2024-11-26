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
        private sealed class StringArrayProcessor : GenericDataProcessor<string[]>
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
                    return "string[]";
                }
            }

            public override int PopPriority => 10;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "string[]",
                    "system.string[]"
                };
            }

            public override string[] Parse(string value)
            {
                return DataTableExtension.ParseArray<string>(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                string[] arr = Parse(value);
                binaryWriter.Write7BitEncodedInt32(arr.Length);
                for (int i = 0; i < arr.Length; i++)
                {
                    binaryWriter.Write(arr[i]);
                }
            }
        }
    }
}
