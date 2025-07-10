using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{

    public class NameScope : NameScopeBase
    {
        private readonly string _namePrefix;
        private readonly List<string> _wordSet;
        private int _nextIndex;

        public NameScope(string namePrefix, List<string> wordSet)
        {
            _namePrefix = namePrefix;
            _wordSet = wordSet;
            _nextIndex = 0;
        }

        protected override void BuildNewName(StringBuilder nameBuilder, string originalName, string lastName)
        {
            nameBuilder.Append(_namePrefix);
            for (int i = _nextIndex++; ;)
            {
                nameBuilder.Append(_wordSet[i % _wordSet.Count]);
                i = i / _wordSet.Count;
                if (i == 0)
                {
                    break;
                }
            }

            // keep generic type name pattern {name}`{n}, if not, il2cpp may raise exception in typeof(G<T>) when G contains a field likes `T a`.
            int index = originalName.LastIndexOf('`');
            if (index != -1)
            {
                nameBuilder.Append(originalName.Substring(index));
            }
        }
    }
}
