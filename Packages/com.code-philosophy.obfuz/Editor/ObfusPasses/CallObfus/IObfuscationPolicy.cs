using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CallObfus
{

    public struct ObfuscationCachePolicy
    {
        public bool cacheInLoop;
        public bool cacheNotInLoop;
    }

    public interface IObfuscationPolicy
    {
        bool NeedObfuscateCallInMethod(MethodDef method);

        ObfuscationCachePolicy GetMethodObfuscationCachePolicy(MethodDef method);

        bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop);
    }

    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscateCallInMethod(MethodDef method);

        public abstract ObfuscationCachePolicy GetMethodObfuscationCachePolicy(MethodDef method);

        public abstract bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop);
    }
}
