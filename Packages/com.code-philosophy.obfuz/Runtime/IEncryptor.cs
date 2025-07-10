using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Obfuz
{
    public interface IEncryptor
    {
        int OpCodeCount { get; }

        void EncryptBlock(byte[] data, int ops, int salt);
        void DecryptBlock(byte[] data, int ops, int salt);

        int Encrypt(int value, int opts, int salt);
        int Decrypt(int value, int opts, int salt);

        long Encrypt(long value, int opts, int salt);
        long Decrypt(long value, int opts, int salt);

        float Encrypt(float value, int opts, int salt);
        float Decrypt(float value, int opts, int salt);

        double Encrypt(double value, int opts, int salt);
        double Decrypt(double value, int opts, int salt);

        byte[] Encrypt(byte[] value, int offset, int length, int opts, int salt);
        byte[] Decrypt(byte[] value, int offset, int byteLength, int ops, int salt);

        byte[] Encrypt(string value, int ops, int salt);
        string DecryptString(byte[] value, int offset, int stringBytesLength, int ops, int salt);
    }
}
