//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;

namespace UGF.EditorTools.Data.DataTable
{
    public sealed partial class DataTableProcessor
    {
        private static class DataProcessorUtility
        {
            private static readonly IDictionary<string, DataProcessor> s_DataProcessors = new SortedDictionary<string, DataProcessor>(StringComparer.OrdinalIgnoreCase);
            private static readonly List<KeyValuePair<int, string>> s_DropdownTypes = new List<KeyValuePair<int, string>>();

            static DataProcessorUtility()
            {
                DataProcessorRegistry.RegisterAll(s_DataProcessors, s_DropdownTypes);
                s_DropdownTypes.Sort((left, right) => left.Key.CompareTo(right.Key));
            }

            public static DataProcessor GetDataProcessor(string type)
            {
                if (type == null)
                {
                    type = string.Empty;
                }

                if (TryGetDataProcessor(type, out DataProcessor dataProcessor))
                {
                    return dataProcessor;
                }

                if (TryResolveCustomJsonType(type, out Type customJsonType))
                {
                    return new CustomJsonProcessor(customJsonType);
                }

                throw new GameFrameworkException(Utility.Text.Format("Not supported data processor type '{0}'.", type));
            }

            internal static bool TryGetDataProcessor(string type, out DataProcessor dataProcessor)
            {
                if (type == null)
                {
                    type = string.Empty;
                }

                return s_DataProcessors.TryGetValue(type, out dataProcessor);
            }

            internal static IList<KeyValuePair<int, string>> GetDropdownTypes()
            {
                return s_DropdownTypes;
            }
        }
    }
}


