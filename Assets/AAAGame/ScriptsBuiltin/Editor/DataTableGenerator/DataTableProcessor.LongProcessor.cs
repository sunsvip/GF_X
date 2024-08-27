//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.IO;

namespace GameFramework.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class LongProcessor : GenericDataProcessor<long>
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
                    return "long";
                }
            }

            public override int PopPriority => 100;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "long",
                    "int64",
                    "system.int64"
                };
            }

            public override long Parse(string value)
            {
                return long.Parse(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write7BitEncodedInt64(Parse(value));
            }
        }
    }
}
