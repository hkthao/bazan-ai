namespace BazanAI.SharedKernel.Security;

/// <summary>
/// Service for encrypting and decrypting Personally Identifiable Information (PII) 
/// before storing it in the database.
/// Uses AES-256-GCM for authenticated encryption.
/// </summary>
public interface IPiiEncryptionService
{
    /// <summary>
    /// Encrypts the given plaintext using AES-256-GCM.
    /// Returns a Base64 string of the format: Base64(IV || Ciphertext || Tag)
    /// </summary>
    /// <param name="plaintext">The text to encrypt</param>
    /// <returns>Base64 encoded encrypted string</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts the given ciphertext.
    /// Expects a Base64 string of the format: Base64(IV || Ciphertext || Tag)
    /// </summary>
    /// <param name="ciphertext">The Base64 encoded encrypted string</param>
    /// <returns>Original plaintext string</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if decryption or authentication fails</exception>
    string Decrypt(string ciphertext);
}
