using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public static class NameMakerFactory
    {
        public static INameMaker CreateDebugNameMaker()
        {
            return new DebugNameMaker();
        }

        public static INameMaker CreateNameMakerBaseASCIICharSet(string namePrefix)
        {
            var words = new List<string>();
            for (int i = 0; i < 26; i++)
            {
                words.Add(((char)('a' + i)).ToString());
                words.Add(((char)('A' + i)).ToString());
            }
            return new WordSetNameMaker(namePrefix, words);
        }
    }
}
