//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.IO;

namespace UGF.EditorTools.Data.DataTable
{
    public sealed partial class DataTableProcessor
    {
        private sealed class DateTimeProcessor : GenericDataProcessor<DateTime>
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
                    return "DateTime";
                }
            }

            public override int ShowOrder => 990;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "datetime",
                    "system.datetime"
                };
            }

            public override DateTime Parse(string value)
            {
                return DataTableExtension.ParseDateTime(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                var dateTime = Parse(value);
                binaryWriter.Write(dateTime.Ticks);
            }
        }
    }
}


