using dnlib.DotNet;
using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public class DebugNameMaker : NameMakerBase
    {
        private class TestNameScope : NameScopeBase
        {
            private int _nextIndex;
            protected override void BuildNewName(StringBuilder nameBuilder, string originalName, string lastName)
            {
                if (string.IsNullOrEmpty(lastName))
                {
                    nameBuilder.Append($"${originalName}");
                }
                else
                {
                    nameBuilder.Append($"${originalName}{_nextIndex++}");
                }
            }
        }

        protected override INameScope CreateNameScope()
        {
            return new TestNameScope();
        }
    }
}
