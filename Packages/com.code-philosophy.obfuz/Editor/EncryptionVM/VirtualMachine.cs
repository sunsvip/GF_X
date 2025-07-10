namespace Obfuz.EncryptionVM
{
    public class VirtualMachine
    {
        public const int SecretKeyLength = 1024;

        public readonly int version;
        public readonly string codeGenerationSecretKey;
        public readonly EncryptionInstructionWithOpCode[] opCodes;

        public VirtualMachine(int version, string codeGenerationSecretKey, EncryptionInstructionWithOpCode[] opCodes)
        {
            this.codeGenerationSecretKey = codeGenerationSecretKey;
            this.opCodes = opCodes;
        }
    }
}
