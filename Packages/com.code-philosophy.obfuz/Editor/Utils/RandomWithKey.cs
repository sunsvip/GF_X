using Obfuz.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public class RandomWithKey : IRandom
    {
        private const long a = 1664525;
        private const long c = 1013904223;
        private const long m = 4294967296; // 2^32

        private readonly int[] _key;

        private int _nextIndex;

        private int _seed;

        public RandomWithKey(int[] key, int seed)
        {
            _key = key;
            _seed = seed;
        }

        public int NextInt(int min, int max)
        {
            return min + NextInt(max - min);
        }

        public int NextInt(int max)
        {
            return (int)((uint)NextInt() % (uint)max);
        }

        private int GetNextSalt()
        {
            if (_nextIndex >= _key.Length)
            {
                _nextIndex = 0;
            }
            return _key[_nextIndex++];
        }

        public int NextInt()
        {
            _seed = (int)((a * _seed + c) % m);
            return _seed ^ GetNextSalt();
        }

        public long NextLong()
        {
            return ((long)NextInt() << 32) | (uint)NextInt();
        }
    }
}
