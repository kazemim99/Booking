// ========================================
// Encryption/IEncryptionService.cs
// ========================================
namespace Booksy.Infrastructure.Security.Encryption;

/// <summary>
/// Encryption service interface
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a string
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a string
    /// </summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Encrypts data
    /// </summary>
    byte[] EncryptData(byte[] data);

    /// <summary>
    /// Decrypts data
    /// </summary>
    byte[] DecryptData(byte[] encryptedData);

    /// <summary>
    /// Hashes a value
    /// </summary>
    string Hash(string value);

    /// <summary>
    /// Verifies a hash
    /// </summary>
    bool VerifyHash(string value, string hash);
}


