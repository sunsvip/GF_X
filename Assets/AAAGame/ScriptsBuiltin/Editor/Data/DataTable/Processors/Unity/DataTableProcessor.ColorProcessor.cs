//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Globalization;
using System.IO;
using UnityEngine;

namespace UGF.EditorTools.Data.DataTable
{
    public sealed partial class DataTableProcessor
    {
        private sealed class ColorProcessor : GenericDataProcessor<Color>
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
                    return "Color";
                }
            }

            public override int ShowOrder => 21;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "color",
                    "unityengine.color"
                };
            }

            public override Color Parse(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Color.white;
                }

                string[] splitedValue = value.Split(',');
                if (splitedValue.Length < 4)
                {
                    return Color.white;
                }

                return new Color(
                    float.Parse(splitedValue[0], CultureInfo.InvariantCulture),
                    float.Parse(splitedValue[1], CultureInfo.InvariantCulture),
                    float.Parse(splitedValue[2], CultureInfo.InvariantCulture),
                    float.Parse(splitedValue[3], CultureInfo.InvariantCulture));
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                Color color = Parse(value);
                binaryWriter.Write(color.r);
                binaryWriter.Write(color.g);
                binaryWriter.Write(color.b);
                binaryWriter.Write(color.a);
            }
        }
    }
}


