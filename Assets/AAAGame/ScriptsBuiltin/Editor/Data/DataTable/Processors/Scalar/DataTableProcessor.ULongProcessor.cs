//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.IO;
using System.Globalization;

namespace UGF.EditorTools.Data.DataTable
{
    public sealed partial class DataTableProcessor
    {
        private sealed class ULongProcessor : GenericDataProcessor<ulong>
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
                    return "ulong";
                }
            }

            public override int ShowOrder => 100;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "ulong",
                    "uint64",
                    "system.uint64"
                };
            }

            public override ulong Parse(string value)
            {
                return ulong.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write7BitEncodedUInt64(Parse(value));
            }
        }
    }
}


