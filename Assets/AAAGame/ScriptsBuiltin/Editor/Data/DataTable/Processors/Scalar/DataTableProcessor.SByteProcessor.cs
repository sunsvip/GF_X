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
        private sealed class SByteProcessor : GenericDataProcessor<sbyte>
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
                    return "sbyte";
                }
            }

            public override int ShowOrder => 999;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "sbyte",
                    "system.sbyte"
                };
            }

            public override sbyte Parse(string value)
            {
                return sbyte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(Parse(value));
            }
        }
    }
}


