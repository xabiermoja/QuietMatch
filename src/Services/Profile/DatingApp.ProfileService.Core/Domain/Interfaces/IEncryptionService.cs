namespace DatingApp.ProfileService.Core.Domain.Interfaces;

/// <summary>
/// Encryption service interface (Port) for field-level encryption.
/// </summary>
/// <remarks>
/// This is a port defined in the Domain layer following Onion Architecture.
/// The concrete implementation (adapter) will be in the Infrastructure layer.
///
/// Used to encrypt/decrypt sensitive PII fields such as FullName.
/// Implementation will use AES-256 encryption with keys stored in Azure Key Vault.
///
/// This interface keeps the domain layer independent of cryptography libraries.
/// </remarks>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plain text using AES-256 encryption.
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>The encrypted cipher text (Base64 encoded)</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts cipher text using AES-256 encryption.
    /// </summary>
    /// <param name="cipherText">The encrypted text (Base64 encoded)</param>
    /// <returns>The decrypted plain text</returns>
    string Decrypt(string cipherText);
}
