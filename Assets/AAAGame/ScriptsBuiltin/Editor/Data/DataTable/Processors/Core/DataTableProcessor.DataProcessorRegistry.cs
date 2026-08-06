//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace UGF.EditorTools.Data.DataTable
{
    public sealed partial class DataTableProcessor
    {
        private static class DataProcessorRegistry
        {
            public static void RegisterAll(IDictionary<string, DataProcessor> dataProcessors, IList<KeyValuePair<int, string>> dropdownTypes)
            {
                Register(dataProcessors, new CommentProcessor());

                Register(dataProcessors, dropdownTypes, new BoolProcessor());
                Register(dataProcessors, dropdownTypes, new ByteProcessor());
                Register(dataProcessors, dropdownTypes, new CharProcessor());
                Register(dataProcessors, dropdownTypes, new DateTimeProcessor());
                Register(dataProcessors, dropdownTypes, new DecimalProcessor());
                Register(dataProcessors, dropdownTypes, new DoubleProcessor());
                Register(dataProcessors, dropdownTypes, new EnumProcessor());
                Register(dataProcessors, dropdownTypes, new FloatProcessor());
                Register(dataProcessors, new IdProcessor());
                Register(dataProcessors, dropdownTypes, new IntProcessor());
                Register(dataProcessors, dropdownTypes, new Int4Processor());
                Register(dataProcessors, dropdownTypes, new LongProcessor());
                Register(dataProcessors, dropdownTypes, new SByteProcessor());
                Register(dataProcessors, dropdownTypes, new ShortProcessor());
                Register(dataProcessors, dropdownTypes, new StringProcessor());
                Register(dataProcessors, dropdownTypes, new TypeProcessor());
                Register(dataProcessors, dropdownTypes, new UIntProcessor());
                Register(dataProcessors, dropdownTypes, new ULongProcessor());
                Register(dataProcessors, dropdownTypes, new UShortProcessor());

                Register(dataProcessors, dropdownTypes, new BoolArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Bool2DArrayProcessor());
                Register(dataProcessors, dropdownTypes, new DoubleArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Double2DArrayProcessor());
                Register(dataProcessors, dropdownTypes, new FloatArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Float2DArrayProcessor());
                Register(dataProcessors, dropdownTypes, new IntArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Int2DArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Int4ArrayProcessor());
                Register(dataProcessors, dropdownTypes, new LongArrayProcessor());
                Register(dataProcessors, dropdownTypes, new StringArrayProcessor());

                Register(dataProcessors, dropdownTypes, new ColorProcessor());
                Register(dataProcessors, dropdownTypes, new Color32Processor());
                Register(dataProcessors, dropdownTypes, new QuaternionProcessor());
                Register(dataProcessors, dropdownTypes, new RectProcessor());
                Register(dataProcessors, dropdownTypes, new Vector2Processor());
                Register(dataProcessors, dropdownTypes, new Vector2ArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Vector2IntProcessor());
                Register(dataProcessors, dropdownTypes, new Vector2IntArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Vector3Processor());
                Register(dataProcessors, dropdownTypes, new Vector3ArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Vector3IntProcessor());
                Register(dataProcessors, dropdownTypes, new Vector3IntArrayProcessor());
                Register(dataProcessors, dropdownTypes, new Vector4Processor());
                Register(dataProcessors, dropdownTypes, new Vector4ArrayProcessor());
            }

            private static void Register(IDictionary<string, DataProcessor> dataProcessors, DataProcessor dataProcessor)
            {
                foreach (var typeString in dataProcessor.GetTypeStrings())
                {
                    if (dataProcessors.ContainsKey(typeString))
                    {
                        throw new InvalidOperationException($"Duplicate data processor type string '{typeString}'.");
                    }

                    dataProcessors.Add(typeString, dataProcessor);
                }
            }

            private static void Register<T>(IDictionary<string, DataProcessor> dataProcessors, IList<KeyValuePair<int, string>> dropdownTypes, GenericDataProcessor<T> dataProcessor)
            {
                Register(dataProcessors, (DataProcessor)dataProcessor);
                dropdownTypes.Add(KeyValuePair.Create(dataProcessor.ShowOrder, dataProcessor.LanguageKeyword));
            }
        }
    }
}


