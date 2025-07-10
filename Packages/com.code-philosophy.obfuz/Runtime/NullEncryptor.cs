using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public class NullEncryptor : EncryptorBase
    {
        private readonly byte[] _key;

        public override int OpCodeCount => 256;

        public NullEncryptor(byte[] key)
        {
            _key = key;
        }

        public override int Encrypt(int value, int opts, int salt)
        {
            return value;
        }

        public override int Decrypt(int value, int opts, int salt)
        {
            return value;
        }

        public override long Encrypt(long value, int opts, int salt)
        {
            return value;
        }

        public override long Decrypt(long value, int opts, int salt)
        {
            return value;
        }

        public override float Encrypt(float value, int opts, int salt)
        {
            return value;
        }

        public override float Decrypt(float value, int opts, int salt)
        {
            return value;
        }

        public override double Encrypt(double value, int opts, int salt)
        {
            return value;
        }

        public override double Decrypt(double value, int opts, int salt)
        {
            return value;
        }

        public override byte[] Encrypt(byte[] value, int offset, int length, int opts, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }
            var encryptedBytes = new byte[length];
            Buffer.BlockCopy(value, offset, encryptedBytes, 0, length);
            return encryptedBytes;
        }

        public override byte[] Decrypt(byte[] value, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }
            byte[] byteArr = new byte[length];
            Buffer.BlockCopy(value, 0, byteArr, 0, length);
            return byteArr;
        }

        public override byte[] Encrypt(string value, int ops, int salt)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public override string DecryptString(byte[] value, int offset, int length, int ops, int salt)
        {
            return Encoding.UTF8.GetString(value, offset, length);
        }
    }
}
