﻿//------------------------------------------------------------
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
        private sealed class Vector2IntProcessor : GenericDataProcessor<Vector2Int>
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
                    return "Vector2Int";
                }
            }

            public override int ShowOrder => 25;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "vector2int",
                    "unityengine.vector2int"
                };
            }

            public override Vector2Int Parse(string value)
            {
                return DataTableExtension.ParseVector2Int(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                var v = Parse(value);
                binaryWriter.Write7BitEncodedInt32(v.x);
                binaryWriter.Write7BitEncodedInt32(v.y);
            }
        }
    }
}
