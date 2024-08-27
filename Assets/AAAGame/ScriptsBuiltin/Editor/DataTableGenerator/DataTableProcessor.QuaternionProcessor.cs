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
        private sealed class QuaternionProcessor : GenericDataProcessor<Quaternion>
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
                    return "Quaternion";
                }
            }

            public override int PopPriority => 990;

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "quaternion",
                    "unityengine.quaternion"
                };
            }

            public override Quaternion Parse(string value)
            {
                return DataTableExtension.ParseQuaternion(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                Quaternion quaternion = Parse(value);
                binaryWriter.Write(quaternion.x);
                binaryWriter.Write(quaternion.y);
                binaryWriter.Write(quaternion.z);
                binaryWriter.Write(quaternion.w);
            }
        }
    }
}
