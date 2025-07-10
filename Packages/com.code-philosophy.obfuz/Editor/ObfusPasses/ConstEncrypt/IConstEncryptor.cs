using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.ConstEncrypt
{
    public interface IConstEncryptor
    {
        void ObfuscateInt(MethodDef method, bool needCacheValue, int value, List<Instruction> obfuscatedInstructions);

        void ObfuscateLong(MethodDef method, bool needCacheValue, long value, List<Instruction> obfuscatedInstructions);

        void ObfuscateFloat(MethodDef method, bool needCacheValue, float value, List<Instruction> obfuscatedInstructions);

        void ObfuscateDouble(MethodDef method, bool needCacheValue, double value, List<Instruction> obfuscatedInstructions);

        void ObfuscateString(MethodDef method, bool needCacheValue, string value, List<Instruction> obfuscatedInstructions);

        void ObfuscateBytes(MethodDef method, bool needCacheValue, byte[] value, List<Instruction> obfuscatedInstructions);
    }

    public abstract class ConstEncryptorBase : IConstEncryptor
    {
        public abstract void ObfuscateBytes(MethodDef method, bool needCacheValue, byte[] value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateDouble(MethodDef method, bool needCacheValue, double value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateFloat(MethodDef method, bool needCacheValue, float value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateInt(MethodDef method, bool needCacheValue, int value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateLong(MethodDef method, bool needCacheValue, long value, List<Instruction> obfuscatedInstructions);
        public abstract void ObfuscateString(MethodDef method, bool needCacheValue, string value, List<Instruction> obfuscatedInstructions);
    }
}
