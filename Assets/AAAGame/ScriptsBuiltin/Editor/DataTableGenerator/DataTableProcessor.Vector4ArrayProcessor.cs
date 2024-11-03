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
        private sealed class Vector4ArrayProcessor : GenericDataProcessor<Vector4[]>
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
                    return "Vector4[]";
                }
            }

            public override int PopPriority => 20;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "vector4[]",
                    "unityengine.vector4[]]"
                };
            }

            public override Vector4[] Parse(string value)
            {
                return DataTableExtension.ParseVector4Array(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(value);
            }
        }
    }
}
