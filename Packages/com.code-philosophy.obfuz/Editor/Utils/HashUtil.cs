using dnlib.DotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Obfuz.Utils
{
    public static class HashUtil
    {
        public static int CombineHash(int hash1, int hash2)
        {
            return hash1 * 1566083941 + hash2;
        }

        public static int ComputeHash(List<TypeSig> sigs)
        {
            int hash = 135781321;
            TypeEqualityComparer tc = TypeEqualityComparer.Instance;
            foreach (var sig in sigs)
            {
                hash = hash * 1566083941 + tc.GetHashCode(sig);
            }
            return hash;
        }

        public static unsafe int ComputeHash(string s)
        {
            fixed (char* ptr = s)
            {
                int num = 352654597;
                int num2 = num;
                int* ptr2 = (int*)ptr;
                int num3;
                for (num3 = s.Length; num3 > 2; num3 -= 4)
                {
                    num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
                    num2 = ((num2 << 5) + num2 + (num2 >> 27)) ^ ptr2[1];
                    ptr2 += 2;
                }

                if (num3 > 0)
                {
                    num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
                }

                return num + num2 * 1566083941;
            }
        }

        public static int ComputePrimitiveOrStringOrBytesHashCode(object obj)
        {
            if (obj is byte[] bytes)
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(bytes);
            }
            if (obj is string s)
            {
                return HashUtil.ComputeHash(s);
            }
            return obj.GetHashCode();
        }
    }
}
