using System;

namespace Obfuz.ObfusPasses
{
    [Flags]
    public enum ObfuscationPassType
    {
        None = 0,

        ConstEncrypt = 0x1,
        FieldEncrypt = 0x2,

        SymbolObfus = 0x100,
        CallObfus = 0x200,
        ExprObfus = 0x400,
        ControlFlowObfus = 0x800,

        AllObfus = SymbolObfus | CallObfus | ExprObfus | ControlFlowObfus,
        AllEncrypt = ConstEncrypt | FieldEncrypt,

        MethodBodyObfusOrEncrypt = ConstEncrypt | CallObfus | ExprObfus | ControlFlowObfus,

        All = ~0,
    }
}
