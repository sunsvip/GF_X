using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.FieldEncrypt
{
    public interface IEncryptPolicy
    {
        bool NeedEncrypt(FieldDef field);
    }

    public abstract class EncryptPolicyBase : IEncryptPolicy
    {
        public abstract bool NeedEncrypt(FieldDef field);
    }
}
