using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Emit
{
    public interface IGroupByModuleEntity
    {
        void Init(ModuleDef mod);
    }

    public abstract class GroupByModuleEntityBase : IGroupByModuleEntity
    {
        public abstract void Init(ModuleDef mod);
    }

    public class GroupByModuleEntityManager
    {
        private readonly Dictionary<(ModuleDef, Type), IGroupByModuleEntity> _moduleEntityManagers = new Dictionary<(ModuleDef, Type), IGroupByModuleEntity>();

        public T GetEntity<T>(ModuleDef mod, Func<T> creator = null) where T : IGroupByModuleEntity
        {
            var key = (mod, typeof(T));
            if (_moduleEntityManagers.TryGetValue(key, out var emitManager))
            {
                return (T)emitManager;
            }
            else
            {
                T newEmitManager;
                if (creator != null)
                {
                    newEmitManager = creator();
                }
                else
                {
                    newEmitManager = (T)Activator.CreateInstance(typeof(T));
                }
                newEmitManager.Init(mod);
                _moduleEntityManagers[key] = newEmitManager;
                return newEmitManager;
            }
        }

        public List<T> GetEntities<T>()  where T: IGroupByModuleEntity
        {
            var managers = new List<T>();
            foreach (var kv in _moduleEntityManagers)
            {
                if (kv.Key.Item2 == typeof(T))
                {
                    managers.Add((T)kv.Value);
                }
            }
            return managers;
        }

        public DefaultMetadataImporter GetDefaultModuleMetadataImporter(ModuleDef module, EncryptionScopeProvider encryptionScopeProvider)
        {
            return GetEntity<DefaultMetadataImporter>(module, () => new DefaultMetadataImporter(encryptionScopeProvider));
        }
    }
}
