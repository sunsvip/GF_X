using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this HashSet<T> values, IEnumerable<T> newValues)
        {
            foreach (var value in newValues)
            {
                values.Add(value);
            }
        }

        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dic, K key)
        {
            return dic.TryGetValue(key, out  V v) ? v : default(V);
        }
    }
}
