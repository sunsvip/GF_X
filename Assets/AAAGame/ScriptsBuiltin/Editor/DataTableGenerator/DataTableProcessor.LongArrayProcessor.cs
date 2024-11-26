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
        private sealed class LongArrayProcessor : GenericDataProcessor<long[]>
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
                    return "long[]";
                }
            }

            public override int PopPriority => 90;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "long[]",
                    "system.long[]"
                };
            }

            public override long[] Parse(string value)
            {
                return DataTableExtension.ParseArray<long>(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(value);
            }
        }
    }
}
