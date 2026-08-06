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
        private sealed class UShortProcessor : GenericDataProcessor<ushort>
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
                    return "ushort";
                }
            }

            public override int ShowOrder => 100;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "ushort",
                    "uint16",
                    "system.uint16"
                };
            }

            public override ushort Parse(string value)
            {
                return ushort.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(Parse(value));
            }
        }
    }
}


