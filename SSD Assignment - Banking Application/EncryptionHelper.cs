using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Banking_Application
{
    public class EncryptionHelper
    {
        private static EncryptionHelper instance = new EncryptionHelper();
        private Aes aes;
        private const string CRYPTO_KEY_NAME = "BankingAppAESKey";

        private EncryptionHelper()
        {
            InitializeEncryption();
        }

        public static EncryptionHelper GetInstance()
        {
            return instance;
        }

        private void InitializeEncryption()
        {
            CngProvider keyStorageProvider = CngProvider.MicrosoftSoftwareKeyStorageProvider;

            if (!CngKey.Exists(CRYPTO_KEY_NAME, keyStorageProvider))
            {
                CngKeyCreationParameters keyCreationParams = new CngKeyCreationParameters()
                {
                    Provider = keyStorageProvider,
                    KeyUsage = CngKeyUsages.AllUsages
                };

                CngKey.Create(new CngAlgorithm("AES"), CRYPTO_KEY_NAME, keyCreationParams);
            }

            aes = new AesCng(CRYPTO_KEY_NAME, keyStorageProvider);
            aes.KeySize = 256; 
            aes.Mode = CipherMode.CBC; 
            aes.Padding = PaddingMode.PKCS7; 
        }

     
        public string EncryptString(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return plaintext; 

            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] iv = new byte[16]; 
            rng.GetBytes(iv);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            aes.IV = iv;

            byte[] ciphertextBytes = Encrypt(plaintextBytes, aes);

            byte[] ivAndCiphertext = new byte[iv.Length + ciphertextBytes.Length];
            Buffer.BlockCopy(iv, 0, ivAndCiphertext, 0, iv.Length);
            Buffer.BlockCopy(ciphertextBytes, 0, ivAndCiphertext, iv.Length, ciphertextBytes.Length);

            return Convert.ToBase64String(ivAndCiphertext);
        }

        public string DecryptString(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64))
                return encryptedBase64; 

            byte[] ivAndCiphertext = Convert.FromBase64String(encryptedBase64);

            byte[] iv = new byte[16];
            Buffer.BlockCopy(ivAndCiphertext, 0, iv, 0, iv.Length);

            byte[] ciphertextBytes = new byte[ivAndCiphertext.Length - iv.Length];
            Buffer.BlockCopy(ivAndCiphertext, iv.Length, ciphertextBytes, 0, ciphertextBytes.Length);

            aes.IV = iv;

            byte[] plaintextBytes = Decrypt(ciphertextBytes, aes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        private byte[] Encrypt(byte[] plaintextData, Aes aes)
        {
            byte[] ciphertextData;
            ICryptoTransform encryptor = aes.CreateEncryptor();

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(plaintextData, 0, plaintextData.Length);
                    csEncrypt.FlushFinalBlock();
                }
                ciphertextData = msEncrypt.ToArray();
            }

            return ciphertextData;
        }

        private byte[] Decrypt(byte[] ciphertextData, Aes aes)
        {
            byte[] plaintextData;
            ICryptoTransform decryptor = aes.CreateDecryptor();

            using (MemoryStream msDecrypt = new MemoryStream())
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                {
                    csDecrypt.Write(ciphertextData, 0, ciphertextData.Length);
                    csDecrypt.FlushFinalBlock();
                }
                plaintextData = msDecrypt.ToArray();
            }

            return plaintextData;
        }
    }
}