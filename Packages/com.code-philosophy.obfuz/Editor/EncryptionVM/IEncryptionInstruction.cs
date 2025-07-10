using System.Collections.Generic;

namespace Obfuz.EncryptionVM
{
    public interface IEncryptionInstruction
    {
        int Encrypt(int value, int[] secretKey, int salt);

        int Decrypt(int value, int[] secretKey, int salt);

        void GenerateEncryptCode(List<string> lines, string indent);

        void GenerateDecryptCode(List<string> lines, string indent);
    }

    public abstract class EncryptionInstructionBase : IEncryptionInstruction
    {
        public abstract int Encrypt(int value, int[] secretKey, int salt);
        public abstract int Decrypt(int value, int[] secretKey, int salt);

        public abstract void GenerateEncryptCode(List<string> lines, string indent);
        public abstract void GenerateDecryptCode(List<string> lines, string indent);
    }
}
