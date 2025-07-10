namespace Obfuz
{
    public interface IEncryptionScope
    {

    }

    public abstract class EncryptionScopeBase : IEncryptionScope
    {
        public void ForcePreserveAOT()
        {
            EncryptionService<EncryptionScopeBase>.Encrypt(0, 0, 0);
        }
    }

    public struct DefaultDynamicEncryptionScope : IEncryptionScope
    {
        public void ForcePreserveAOT()
        {
            EncryptionService<DefaultDynamicEncryptionScope>.Encrypt(0, 0, 0);
        }
    }

    public struct DefaultStaticEncryptionScope: IEncryptionScope
    {
        public void ForcePreserveAOT()
        {
            EncryptionService<DefaultStaticEncryptionScope>.Encrypt(0, 0, 0);
        }
    }
}
