#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public sealed class EsfmDecryptor
    {
        private static readonly byte[] TrophyMasterKey =
        {
            0x21, 0xF4, 0x1A, 0x6B, 0xAD, 0x8A, 0x1D, 0x3E,
            0xCA, 0x7A, 0xD5, 0x86, 0xC1, 0x01, 0xB7, 0xA9
        };

        private readonly NpCommunicationIdResolver _idResolver = new();

        public byte[] Decrypt(ReadOnlySpan<byte> encryptedData, string npCommunicationId)
        {
            if (!_idResolver.IsValid(npCommunicationId))
                throw new ArgumentException("NP Communication ID must match NPWRxxxxx_00.", nameof(npCommunicationId));
            if (encryptedData.Length < 32 || encryptedData.Length % 16 != 0)
                throw new InvalidDataException("ESFM data must contain at least two complete AES blocks.");

            byte[] titleKey = DeriveTitleKey(npCommunicationId);
            byte[] decrypted;
            try
            {
                using Aes aes = CreateAes(titleKey, PaddingMode.PKCS7);
                using ICryptoTransform transform = aes.CreateDecryptor();
                byte[] encrypted = encryptedData.ToArray();
                decrypted = transform.TransformFinalBlock(encrypted, 0, encrypted.Length);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("ESFM decryption failed. The NP Communication ID may be incorrect or the payload is damaged.", ex);
            }

            if (decrypted.Length < 17 || !decrypted.AsSpan(0, 16).SequenceEqual(new byte[16]))
                throw new CryptographicException("ESFM decryption did not produce the required 16-byte zero prefix. The NP Communication ID is incorrect or this ESFM variant is unsupported.");

            byte[] xml = decrypted.AsSpan(16).ToArray();
            try
            {
                _ = new UTF8Encoding(false, true).GetString(xml);
            }
            catch (DecoderFallbackException ex)
            {
                throw new InvalidDataException("Decrypted ESFM data is not valid UTF-8.", ex);
            }
            return xml;
        }

        public byte[] DeriveTitleKey(string npCommunicationId)
        {
            if (!_idResolver.IsValid(npCommunicationId))
                throw new ArgumentException("NP Communication ID must match NPWRxxxxx_00.", nameof(npCommunicationId));
            byte[] input = new byte[16];
            int written = Encoding.ASCII.GetBytes(npCommunicationId, input);
            if (written != 12)
                throw new ArgumentException("NP Communication ID must be exactly 12 ASCII bytes.", nameof(npCommunicationId));

            using Aes aes = CreateAes(TrophyMasterKey, PaddingMode.None);
            using ICryptoTransform transform = aes.CreateEncryptor();
            return transform.TransformFinalBlock(input, 0, input.Length);
        }

        private static Aes CreateAes(byte[] key, PaddingMode padding)
        {
            if (key.Length != 16)
                throw new ArgumentException("AES-128 requires a 16-byte key.", nameof(key));
            Aes aes = Aes.Create();
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = padding;
            aes.Key = key;
            aes.IV = new byte[16];
            return aes;
        }
    }
}
