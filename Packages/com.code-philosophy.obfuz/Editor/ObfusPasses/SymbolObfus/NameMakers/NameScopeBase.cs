using System.Collections.Generic;
using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public abstract class NameScopeBase : INameScope
    {

        private readonly Dictionary<string, string> _nameMap = new Dictionary<string, string>();

        private readonly HashSet<string> _preservedNames = new HashSet<string>();


        public bool AddPreservedName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                return _preservedNames.Add(name);
            }
            return false;
        }

        public bool IsNamePreserved(string name)
        {
            return _preservedNames.Contains(name);
        }


        protected abstract void BuildNewName(StringBuilder nameBuilder, string originalName, string lastName);

        private string CreateNewName(string originalName)
        {
            var nameBuilder = new StringBuilder();
            string lastName = null;
            while (true)
            {
                nameBuilder.Clear();
                BuildNewName(nameBuilder, originalName, lastName);
                string newName = nameBuilder.ToString();
                lastName = newName;
                if (_preservedNames.Add(newName))
                {
                    return newName;
                }
            }
        }

        public string GetNewName(string originalName, bool reuse)
        {
            if (!reuse)
            {
                return CreateNewName(originalName);
            }
            if (_nameMap.TryGetValue(originalName, out var newName))
            {
                return newName;
            }
            newName = CreateNewName(originalName);
            _nameMap[originalName] = newName;
            return newName;
        }
    }
}
