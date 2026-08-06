//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.IO;
using System.Globalization;
using UnityEngine;

namespace UGF.EditorTools.Data.DataTable
{
    public sealed partial class DataTableProcessor
    {
        private sealed class Color32Processor : GenericDataProcessor<Color32>
        {
            public override bool IsSystem
            {
                get
                {
                    return false;
                }
            }

            public override string LanguageKeyword
            {
                get
                {
                    return "Color32";
                }
            }

            public override int ShowOrder => 21;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "color32",
                    "unityengine.color32"
                };
            }

            public override Color32 Parse(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return new Color32(255, 255, 255, 255);
                }

                string[] splitedValue = value.Split(',');
                if (splitedValue.Length < 4)
                {
                    return new Color32(255, 255, 255, 255);
                }

                return new Color32(
                    byte.Parse(splitedValue[0], NumberStyles.Integer, CultureInfo.InvariantCulture),
                    byte.Parse(splitedValue[1], NumberStyles.Integer, CultureInfo.InvariantCulture),
                    byte.Parse(splitedValue[2], NumberStyles.Integer, CultureInfo.InvariantCulture),
                    byte.Parse(splitedValue[3], NumberStyles.Integer, CultureInfo.InvariantCulture));
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                Color32 color32 = Parse(value);
                binaryWriter.Write(color32.r);
                binaryWriter.Write(color32.g);
                binaryWriter.Write(color32.b);
                binaryWriter.Write(color32.a);
            }
        }
    }
}


