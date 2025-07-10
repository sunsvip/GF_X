using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Obfuz.EncryptionVM
{

    public class VirtualMachineSimulator : EncryptorBase
    {
        private readonly EncryptionInstructionWithOpCode[] _opCodes;
        private readonly int[] _secretKey;

        public override int OpCodeCount => _opCodes.Length;

        public VirtualMachineSimulator(VirtualMachine vm, byte[] byteSecretKey)
        {
            _opCodes = vm.opCodes;
            _secretKey = KeyGenerator.ConvertToIntKey(byteSecretKey);

            VerifyInstructions();
        }

        private void VerifyInstructions()
        {
            int value = 0x11223344;
            for (int i = 0; i < _opCodes.Length; i++)
            {
                int encryptedValue = _opCodes[i].Encrypt(value, _secretKey, i);
                int decryptedValue = _opCodes[i].Decrypt(encryptedValue, _secretKey, i);
                //Debug.Log($"instruction type:{_opCodes[i].function.GetType()}");
                Assert.AreEqual(value, decryptedValue);
            }

            int ops = 11223344;
            int salt = 789;
            Assert.AreEqual(1, Decrypt(Encrypt(1, ops, salt), ops, salt));
            Assert.AreEqual(1L, Decrypt(Encrypt(1L, ops, salt), ops, salt));
            Assert.AreEqual(1.0f, Decrypt(Encrypt(1.0f, ops, salt), ops, salt));
            Assert.AreEqual(1.0, Decrypt(Encrypt(1.0, ops, salt), ops, salt));

            byte[] strBytes = Encrypt("abcdef", ops, salt);
            Assert.AreEqual("abcdef", DecryptString(strBytes, 0, strBytes.Length, ops, salt));
            var arr = new byte[100];
            for (int i = 0; i < arr.Length ; i++)
            {
                arr[i] = (byte)i;
            }
            EncryptBlock(arr, ops, salt);
            DecryptBlock(arr, ops, salt);
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.AreEqual(i, arr[i]);
            }
        }

        private List<uint> DecodeOps(uint ops)
        {
            var codes = new List<uint>();
            while (ops != 0)
            {
                uint code = ops % (uint)_opCodes.Length;
                codes.Add(code);
                ops /= (uint)_opCodes.Length;
            }
            return codes;
        }

        public override int Encrypt(int value, int ops, int salt)
        {
            var codes = DecodeOps((uint)ops);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                var opCode = _opCodes[codes[i]];
                value = opCode.Encrypt(value, _secretKey, salt);
            }
            return value;
        }

        public override int Decrypt(int value, int ops, int salt)
        {
            var codes = DecodeOps((uint)ops);
            foreach (var code in codes)
            {
                var opCode = _opCodes[code];
                value = opCode.Decrypt(value, _secretKey, salt);
            }
            return value;
        }
    }
}
