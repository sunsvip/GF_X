using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public abstract class AssemblyResolverBase : IAssemblyResolver
    {
        public abstract string ResolveAssembly(string assemblyName);
    }
}
