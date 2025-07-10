using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{

    public class WordSetNameMaker : NameMakerBase
    {
        private readonly string _namePrefix;
        private readonly List<string> _wordSet;

        public WordSetNameMaker(string namePrefix, List<string> wordSet)
        {
            _namePrefix = namePrefix;
            _wordSet = wordSet;
        }

        protected override INameScope CreateNameScope()
        {
            return new NameScope(_namePrefix, _wordSet);
        }
    }
}
