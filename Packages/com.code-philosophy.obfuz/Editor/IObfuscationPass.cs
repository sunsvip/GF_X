using Obfuz.ObfusPasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public interface IObfuscationPass
    {
        ObfuscationPassType Type { get; }

        void Start();

        void Stop();

        void Process();
    }
}
