//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        public abstract class GenericDataProcessor<T> : DataProcessor
        {
            /// <summary>
            /// Excel表中在下拉列表中展示的优先级
            /// </summary>
            public abstract int ShowOrder { get; }
            public override System.Type Type
            {
                get
                {
                    return typeof(T);
                }
            }

            public override bool IsId
            {
                get
                {
                    return false;
                }
            }

            public override bool IsComment
            {
                get
                {
                    return false;
                }
            }

            public abstract T Parse(string value);
        }
    }
}
