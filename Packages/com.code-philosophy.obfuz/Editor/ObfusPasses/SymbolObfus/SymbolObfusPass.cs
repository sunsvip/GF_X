using Obfuz.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public class SymbolObfusPass : ObfuscationPassBase
    {
        private SymbolRename _symbolRename;

        public override ObfuscationPassType Type => ObfuscationPassType.SymbolObfus;

        public SymbolObfusPass(SymbolObfuscationSettingsFacade settings)
        {
            _symbolRename = new SymbolRename(settings);
        }

        public override void Start()
        {
            _symbolRename.Init();
        }

        public override void Stop()
        {
            _symbolRename.Save();
        }

        public override void Process()
        {
            _symbolRename.Process();
        }
    }
}
