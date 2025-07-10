using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public class CombinedAssemblyResolver : AssemblyResolverBase
    {
        private readonly List<IAssemblyResolver> _resolvers;

        public CombinedAssemblyResolver(params IAssemblyResolver[] resolvers)
        {
            _resolvers = resolvers.ToList();
        }

        public override string ResolveAssembly(string assemblyName)
        {
            foreach (var resolver in _resolvers)
            {
                var assemblyPath = resolver.ResolveAssembly(assemblyName);
                if (assemblyPath != null)
                {
                    return assemblyPath;
                }
            }
            return null;
        }

        public void InsertFirst(IAssemblyResolver resolver)
        {
            _resolvers.Insert(0, resolver);
        }
    }
}
