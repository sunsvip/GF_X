using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public class AssemblyCache
    {
        private readonly IAssemblyResolver _assemblyPathResolver;
        private readonly ModuleContext _modCtx;
        private readonly AssemblyResolver _asmResolver;
        private bool _enableTypeDefCache;


        public ModuleContext ModCtx => _modCtx;

        public Dictionary<string, ModuleDefMD> LoadedModules { get; } = new Dictionary<string, ModuleDefMD>();

        public AssemblyCache(IAssemblyResolver assemblyResolver)
        {
            _enableTypeDefCache = true;
            _assemblyPathResolver = assemblyResolver;
            _modCtx = ModuleDef.CreateModuleContext();
            _asmResolver = (AssemblyResolver)_modCtx.AssemblyResolver;
            _asmResolver.EnableTypeDefCache = _enableTypeDefCache;
            _asmResolver.UseGAC = false;
        }

        public bool EnableTypeDefCache
        {
            get => _enableTypeDefCache;
            set
            {
                _enableTypeDefCache = value;
                _asmResolver.EnableTypeDefCache = value;
                foreach (var mod in LoadedModules.Values)
                {
                    mod.EnableTypeDefFindCache = value;
                }
            }
        }


        public ModuleDefMD TryLoadModule(string moduleName)
        {
            string dllPath = _assemblyPathResolver.ResolveAssembly(moduleName);
            if (string.IsNullOrEmpty(dllPath))
            {
                return null;
            }
            return LoadModule(moduleName);
        }

        public ModuleDefMD LoadModule(string moduleName)
        {
            // Debug.Log($"load module:{moduleName}");
            if (LoadedModules.TryGetValue(moduleName, out var mod))
            {
                return mod;
            }
            string assemblyPath = _assemblyPathResolver.ResolveAssembly(moduleName);
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new FileNotFoundException($"Assembly {moduleName} not found");
            }
            mod = DoLoadModule(assemblyPath);
            LoadedModules.Add(moduleName, mod);


            foreach (var refAsm in mod.GetAssemblyRefs())
            {
                LoadModule(refAsm.Name);
            }

            return mod;
        }

        private ModuleDefMD DoLoadModule(string dllPath)
        {
            //Debug.Log($"do load module:{dllPath}");
            ModuleDefMD mod = ModuleDefMD.Load(File.ReadAllBytes(dllPath), _modCtx);
            mod.EnableTypeDefFindCache = _enableTypeDefCache;
            _asmResolver.AddToCache(mod);
            return mod;
        }
    }
}
