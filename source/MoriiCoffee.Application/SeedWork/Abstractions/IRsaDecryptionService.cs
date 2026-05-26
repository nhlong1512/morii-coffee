namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>Decrypts RSA-OAEP (SHA-256) ciphertext produced by the frontend using the matching public key.</summary>
public interface IRsaDecryptionService
{
    /// <summary>Decrypts a Base64-encoded RSA-OAEP ciphertext and returns the original plaintext password.</summary>
    string Decrypt(string base64Ciphertext);
}
