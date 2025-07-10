using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.ConstEncrypt
{
    public struct ConstCachePolicy
    {
        public bool cacheConstInLoop;
        public bool cacheConstNotInLoop;
        public bool cacheStringInLoop;
        public bool cacheStringNotInLoop;
    }

    public interface IEncryptPolicy
    {
        bool NeedObfuscateMethod(MethodDef method);

        ConstCachePolicy GetMethodConstCachePolicy(MethodDef method);

        bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value);

        bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value);

        bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value);

        bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value);

        bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value);

        bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array);
    }

    public abstract class EncryptPolicyBase : IEncryptPolicy
    {
        public abstract bool NeedObfuscateMethod(MethodDef method);
        public abstract ConstCachePolicy GetMethodConstCachePolicy(MethodDef method);
        public abstract bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value);
        public abstract bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value);
        public abstract bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value);
        public abstract bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value);
        public abstract bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value);
        public abstract bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array);
    }
}
