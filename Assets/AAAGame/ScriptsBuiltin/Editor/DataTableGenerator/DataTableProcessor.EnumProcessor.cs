//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.IO;
using UnityEngine;

namespace GameFramework.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class EnumProcessor : GenericDataProcessor<int>
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
                    return "enum";
                }
            }

            public override int PopPriority => 10;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "enum",
                    "system.enum"
                };
            }

            public override int Parse(string value)
            {
                var enumElements = value.Split('.');
                if (enumElements.Length != 2)
                {
                    GFBuiltin.LogError(Utility.Text.Format("解析枚举类型失败:{0}, 配置枚举格式为: Enum.Item1", value));
                    return 0;
                }
                var enumType = Type.GetType(enumElements[0]);
                return (int)Enum.Parse(enumType, value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write7BitEncodedInt32(Parse(value));
            }
        }
    }
}
