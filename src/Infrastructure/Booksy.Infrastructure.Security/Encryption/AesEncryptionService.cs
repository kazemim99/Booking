// ========================================
// Encryption/IEncryptionService.cs
// ========================================
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Booksy.Infrastructure.Security.Encryption;


/// <summary>
/// AES encryption service implementation
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<AesEncryptionService> _logger;

    public AesEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AesEncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector("Booksy.Encryption");
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            return _protector.Protect(plainText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt text");
            throw;
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt text");
            throw;
        }
    }

    public byte[] EncryptData(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();

        // Write key and IV to the beginning of the stream
        ms.Write(aes.Key, 0, aes.Key.Length);
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }

        return ms.ToArray();
    }

    public byte[] DecryptData(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
        {
            return encryptedData;
        }

        using var aes = Aes.Create();

        // Read key and IV from the beginning of the data
        var key = new byte[32]; // 256 bits
        var iv = new byte[16];  // 128 bits

        Array.Copy(encryptedData, 0, key, 0, key.Length);
        Array.Copy(encryptedData, key.Length, iv, 0, iv.Length);

        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encryptedData, key.Length + iv.Length, encryptedData.Length - key.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var resultMs = new MemoryStream();

        cs.CopyTo(resultMs);
        return resultMs.ToArray();
    }

    public string Hash(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyHash(string value, string hash)
    {
        var computedHash = Hash(value);
        return computedHash == hash;
    }
}