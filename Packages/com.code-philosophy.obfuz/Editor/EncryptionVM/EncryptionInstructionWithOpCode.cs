namespace Obfuz.EncryptionVM
{
    public class EncryptionInstructionWithOpCode
    {
        public readonly ushort code;

        public readonly IEncryptionInstruction function;

        public EncryptionInstructionWithOpCode(ushort code, IEncryptionInstruction function)
        {
            this.code = code;
            this.function = function;
        }

        public int Encrypt(int value, int[] secretKey, int salt)
        {
            return function.Encrypt(value, secretKey, salt);
        }

        public int Decrypt(int value, int[] secretKey, int salt)
        {
            return function.Decrypt(value, secretKey, salt);
        }
    }
}
