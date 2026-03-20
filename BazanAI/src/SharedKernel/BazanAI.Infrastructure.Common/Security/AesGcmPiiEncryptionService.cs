using System;
using System.Security.Cryptography;
using System.Text;
using BazanAI.SharedKernel.Security;
using Microsoft.Extensions.Configuration;

namespace BazanAI.Infrastructure.Common.Security;

public class AesGcmPiiEncryptionService : IPiiEncryptionService
{
    private readonly byte[] _key;
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16;   // 128 bits for GCM

    public AesGcmPiiEncryptionService(IConfiguration configuration)
    {
        var keyHex = configuration["PII_ENCRYPTION_KEY"];
        if (string.IsNullOrWhiteSpace(keyHex))
        {
            throw new InvalidOperationException("PII_ENCRYPTION_KEY is not configured in environment variables.");
        }

        try
        {
            _key = Convert.FromHexString(keyHex);
            if (_key.Length != 32)
            {
                throw new InvalidOperationException("PII_ENCRYPTION_KEY must be a 32-byte hex string (64 characters).");
            }
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("PII_ENCRYPTION_KEY must be a valid hex string.", ex);
        }
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] inputBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[inputBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, inputBytes, ciphertext, tag);

        // Format: Base64(Nonce || Ciphertext || Tag)
        byte[] result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

        try
        {
            byte[] fullBytes = Convert.FromBase64String(ciphertext);

            if (fullBytes.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("Invalid ciphertext length.");
            }

            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            int ciphertextLength = fullBytes.Length - NonceSize - TagSize;
            byte[] cipherBytes = new byte[ciphertextLength];

            Buffer.BlockCopy(fullBytes, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(fullBytes, NonceSize, cipherBytes, 0, ciphertextLength);
            Buffer.BlockCopy(fullBytes, NonceSize + ciphertextLength, tag, 0, TagSize);

            byte[] decryptedBytes = new byte[ciphertextLength];

            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Decrypt(nonce, cipherBytes, tag, decryptedBytes);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            // Log warning here in a real scenario
            throw new CryptographicException("Decryption failed. The data might be tampered or the key is incorrect.", ex);
        }
    }
}
