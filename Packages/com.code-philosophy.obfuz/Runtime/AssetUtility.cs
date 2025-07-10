using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public static class AssetUtility
    {
        public static void VerifySecretKey(int expectedValue, int actualValue)
        {
            if (expectedValue != actualValue)
            {
                throw new Exception($"VerifySecretKey failed. Your secret key is unmatched with secret key used by current assembly in obfuscation");
            }
        }
    }
}
