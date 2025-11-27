using System.Security.Cryptography;
using System.Text;
using DatingApp.ProfileService.Core.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DatingApp.ProfileService.Infrastructure.Security;

/// <summary>
/// AES-256 encryption service for field-level encryption (adapter for IEncryptionService port).
/// </summary>
/// <remarks>
/// Implements IEncryptionService port defined in Domain layer.
///
/// Security Features:
/// - AES-256 encryption algorithm
/// - Unique IV (Initialization Vector) per encryption (stored with ciphertext)
/// - Base64 encoding for storage
/// - Key rotation support (key ID prefix in ciphertext)
/// - Key stored in Azure Key Vault (via IConfiguration)
///
/// Ciphertext Format: {keyId}:{IV}:{ciphertext}
/// Example: v1:abc123...:xyz789...
///
/// This allows for key rotation - old data encrypted with v1 can still be decrypted,
/// while new data uses v2 key.
///
/// IMPORTANT: In production, encryption keys MUST be stored in Azure Key Vault, not appsettings.
/// </remarks>
public class EncryptionService : IEncryptionService
{
    private const string KeyIdPrefix = "v1";
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        // In production: Fetch from Azure Key Vault
        // For now: From appsettings (Base64-encoded 256-bit key)
        var keyString = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured. Set Encryption:Key in appsettings or Azure Key Vault.");

        _key = Convert.FromBase64String(keyString);

        if (_key.Length != 32) // 256 bits = 32 bytes
        {
            throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes). Current key length: " + _key.Length);
        }
    }

    /// <summary>
    /// Encrypts plain text using AES-256.
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Encrypted text in format: {keyId}:{IV}:{ciphertext}</returns>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV(); // Unique IV for each encryption

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        var cipherBytes = ms.ToArray();
        var iv = Convert.ToBase64String(aes.IV);
        var cipherText = Convert.ToBase64String(cipherBytes);

        // Format: {keyId}:{IV}:{ciphertext}
        return $"{KeyIdPrefix}:{iv}:{cipherText}";
    }

    /// <summary>
    /// Decrypts cipher text using AES-256.
    /// </summary>
    /// <param name="cipherText">The encrypted text in format: {keyId}:{IV}:{ciphertext}</param>
    /// <returns>Decrypted plain text</returns>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            // Parse format: {keyId}:{IV}:{ciphertext}
            var parts = cipherText.Split(':', 3);
            if (parts.Length != 3)
            {
                throw new CryptographicException("Invalid ciphertext format. Expected: {keyId}:{IV}:{ciphertext}");
            }

            var keyId = parts[0];
            var ivString = parts[1];
            var cipherString = parts[2];

            // Key rotation support: use appropriate key based on keyId
            // For now, only v1 is supported
            if (keyId != KeyIdPrefix)
            {
                throw new CryptographicException($"Unsupported key ID: {keyId}. Current key ID: {KeyIdPrefix}");
            }

            var iv = Convert.FromBase64String(ivString);
            var cipherBytes = Convert.FromBase64String(cipherString);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Decryption failed. The data may be corrupted or the key may be incorrect.", ex);
        }
    }
}
