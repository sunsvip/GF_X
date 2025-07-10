using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Utils
{
    public static class EncryptionUtil
    {
        public static int GetBitCount(int value)
        {
            int count = 0;
            while (value > 0)
            {
                count++;
                value >>= 1;
            }
            return count;
        }

        public static int GenerateEncryptionOpCodes(IRandom random, IEncryptor encryptor, int encryptionLevel)
        {
            if (encryptionLevel <= 0 || encryptionLevel > 4)
            {
                throw new ArgumentException($"Invalid encryption level: {encryptionLevel}, should be in range [1,4]");
            }
            int vmOpCodeCount = encryptor.OpCodeCount;
            long ops = 0;
            for (int i = 0; i < encryptionLevel; i++)
            {
                long newOps = ops * vmOpCodeCount;
                // don't use 0
                int op = random.NextInt(1, vmOpCodeCount);
                newOps |= (uint)op;
                if (newOps > uint.MaxValue)
                {
                    Debug.LogWarning($"OpCode overflow. encryptionLevel:{encryptionLevel}, vmOpCodeCount:{vmOpCodeCount}");
                }
                else
                {
                    ops = newOps;
                }
            }
            return (int)ops;
        }
    }
}
