﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public interface IAssemblyResolver
    {
        string ResolveAssembly(string assemblyName);
    }
}
