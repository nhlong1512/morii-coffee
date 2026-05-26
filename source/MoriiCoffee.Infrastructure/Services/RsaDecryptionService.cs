using System.Security.Cryptography;
using System.Text;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// Decrypts RSA-OAEP (SHA-256) ciphertext using the configured PKCS#8 private key.
/// Registered as a singleton — the RSA instance is created once and reused.
/// </summary>
public sealed class RsaDecryptionService : IRsaDecryptionService, IDisposable
{
    private readonly RSA _rsa;

    public RsaDecryptionService(RsaSettings settings)
    {
        _rsa = RSA.Create();
        var privateKey = settings.PrivateKey.Replace("\\n", "\n").Trim();
        _rsa.ImportFromPem(privateKey);
    }

    public string Decrypt(string base64Ciphertext)
    {
        var cipherBytes = Convert.FromBase64String(base64Ciphertext);
        var plainBytes = _rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public void Dispose() => _rsa.Dispose();
}
