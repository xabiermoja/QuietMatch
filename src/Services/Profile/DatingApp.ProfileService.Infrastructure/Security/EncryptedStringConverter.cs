using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DatingApp.ProfileService.Infrastructure.Security;

/// <summary>
/// EF Core value converter for transparent field-level encryption.
/// </summary>
/// <remarks>
/// Automatically encrypts/decrypts string properties when reading/writing to database.
///
/// Usage in entity configuration:
/// builder.Property(p => p.FullName)
///     .HasConversion<EncryptedStringConverter>();
///
/// This provides transparent encryption - the domain model works with plaintext,
/// but the database stores encrypted data.
///
/// IMPORTANT: The EncryptionService must be registered as a singleton in DI
/// so this converter can access it via static field (workaround for EF Core limitation).
/// </remarks>
public class EncryptedStringConverter : ValueConverter<string, string>
{
    // Static field to hold the encryption service (set during DI configuration)
    // This is a workaround because EF Core value converters can't use DI directly
    private static Core.Domain.Interfaces.IEncryptionService? _encryptionService;

    public static void Initialize(Core.Domain.Interfaces.IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public EncryptedStringConverter()
        : base(
            plainText => Encrypt(plainText),
            cipherText => Decrypt(cipherText))
    {
    }

    private static string Encrypt(string plainText)
    {
        if (_encryptionService == null)
        {
            throw new InvalidOperationException(
                "EncryptedStringConverter not initialized. " +
                "Call EncryptedStringConverter.Initialize(encryptionService) during application startup.");
        }

        return _encryptionService.Encrypt(plainText);
    }

    private static string Decrypt(string cipherText)
    {
        if (_encryptionService == null)
        {
            throw new InvalidOperationException(
                "EncryptedStringConverter not initialized. " +
                "Call EncryptedStringConverter.Initialize(encryptionService) during application startup.");
        }

        return _encryptionService.Decrypt(cipherText);
    }
}
