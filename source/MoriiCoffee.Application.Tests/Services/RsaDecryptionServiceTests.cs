using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Infrastructure.Services;
using Xunit;

namespace MoriiCoffee.Application.Tests.Services;

public class RsaDecryptionServiceTests
{
    // PKCS#8 private key used only in tests — NOT a production key.
    private const string TestPrivateKeyPem =
        "-----BEGIN PRIVATE KEY-----\n" +
        "MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCaThjQHF7tHE+t\n" +
        "ZPC8ERlGil97Z/MIuUnkQ8FwXAP4D/cgc7n9Te/kfTKYf0NRf9P6kanIfjXNV2WT\n" +
        "STrd45ndT/6mzy3TfOfbAbJjskoU5nZhqbJ+kFsdUS+dclLd+xD7XLYcKYqkQEaP\n" +
        "7+dSNQrrAkjJElvTt0E0ats17vq2IsLv4YTO6KGPuaY56QYZfejdegxD/MiDgfJj\n" +
        "oU1uBqmV54CijByDdhb0XD+Sdx+CpWnjyuQdROcofxddTYRwsYvJjD7rqBMEDFYu\n" +
        "nKJiptL+pK2GrYFXcLIaZ/rXgXHVoow2xzax4upf+N+hIhT3Wu+3Fja6lp+2tHQ5\n" +
        "uZCDHAF1AgMBAAECggEAEJPtZIZ3yF+vS8C5gGsR3RtGsNp1KO3HO4fwA2NPZdpJ\n" +
        "QapNRCKYcGLnCa06jUn/ez8lD45Ht3z5Q76tXWNzh2xtvnwpvzv/KO9gvAdOoDo2\n" +
        "Y724mJJnx5mOVQsQThsIMwk9436vD4B8VECBCLr8Jk2Dhl67kN5yWfBVBtFbDDA7\n" +
        "C7fzQWekTcNVz7GoEWMHjUAC5Yy5lBcoCzTMQdmSTlgwLE3P+vUcGs8AyAnjV0Z1\n" +
        "M5eNatNJzI5kVqEoHlKZ15KRJRt8CbFGhB4q2YxdtMewXB96/5OGs/jUZXyh0bBe\n" +
        "+7GsjxxQ2lxnFFRY4Kx9VJ6sQUFwk1ChXDt/sDBZjQKBgQDXOxtTp7ve7LQ7R0Oq\n" +
        "+N+8IAoZ/GP2t3KaOCMF5kRGbmRrgS+XPtjeeYjMK3tCEvcBF3iExxi1lpIb1dxs\n" +
        "DDeKDXfVoCNnth+IZIQJeDvXto0a6u9OWeJofUKt+uIYEYerlvUGTIBmlIK6gYG9\n" +
        "JzKJcHnJlcAiqZ2pJ96tescLNwKBgQC3iJlKTNiQjnSUwk76FceNjWxzXkMU4pyg\n" +
        "6PaJUMqikQrvce6r2kx3WLfCstbyhKrdgcu8lRVfyYwXTmCZAuaRMuuzOT5Pa4u4\n" +
        "GwFUPrBhcZAPQIZ+XAhL4uXzTKRSFS1uo1w7o31ZRGFnmLiM2/9QdO598bFMxqe6\n" +
        "294peGMmswKBgFxIl5ry3Hbk/xI7qCPyudur0Sj7MtFiLt05HKs25CdexefiaElt\n" +
        "RQd/DMyeCCd4gjgRnDcyNsIFYXhV5kDdrCKhS7RpCUU6raKJlqOIzf/b4fycpybt\n" +
        "G0q6CpEWdULkoUtNWpnsy1EwdC0LwlkcKWsMsutgLhWurE8PLUcs0ZNZAoGAKcE8\n" +
        "DrlY89pVD5r9WMwnsD6ik8S4QkIkHD+kBy2ITF/vOvaStCpgBy7576O8X0RrkyV6\n" +
        "cpcAW+CArLS6KVWNmy8YjJfTY0I2cVZDgSUZ/7FUcwPdFVZe1NT0N9wR7lK/GVK1\n" +
        "IyRY2jxCZM1L/0/10BoqQCECk1MGye5Hpuuqsx0CgYEA1asgk8dLTDIzEJXUQCUl\n" +
        "nMNSNlcAp4H1cukTd/pKrnni3MrrmAV01s6XtZsuBuJUlAM+NgI63ls0i9Kuk905\n" +
        "SdErnnG9i8Ht0D94fColh6DOxT0GpVwJIusfi0UBj1AH8MDoaGcYCoJX0DrRyWSs\n" +
        "rUJQ88+dQjOP21bnGR6isjc=\n" +
        "-----END PRIVATE KEY-----\n";

    private static string EncryptWithPublicKey(string plaintext)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(TestPrivateKeyPem);
        var spki = rsa.ExportSubjectPublicKeyInfo();

        using var rsaPub = RSA.Create();
        rsaPub.ImportSubjectPublicKeyInfo(spki, out _);

        var cipherBytes = rsaPub.Encrypt(Encoding.UTF8.GetBytes(plaintext), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(cipherBytes);
    }

    private static RsaDecryptionService CreateService() =>
        new(new RsaSettings { PrivateKey = TestPrivateKeyPem });

    [Fact]
    public void Decrypt_ValidCiphertext_ReturnsOriginalPlaintext()
    {
        const string plaintext = "TestPassword1!";
        var ciphertext = EncryptWithPublicKey(plaintext);
        var service = CreateService();

        var result = service.Decrypt(ciphertext);

        result.Should().Be(plaintext);
    }

    [Fact]
    public void Decrypt_AnotherValue_ReturnsCorrectResult()
    {
        const string plaintext = "AnotherS3cur3Pass!";
        var ciphertext = EncryptWithPublicKey(plaintext);
        var service = CreateService();

        var result = service.Decrypt(ciphertext);

        result.Should().Be(plaintext);
    }

    [Fact]
    public void Decrypt_UnicodePassword_RoundTripsCorrectly()
    {
        const string plaintext = "Pässw0rd!Ünïcödé";
        var ciphertext = EncryptWithPublicKey(plaintext);
        var service = CreateService();

        var result = service.Decrypt(ciphertext);

        result.Should().Be(plaintext);
    }

    [Fact]
    public void Decrypt_InvalidBase64_ThrowsFormatException()
    {
        var service = CreateService();

        var act = () => service.Decrypt("not-valid-base64!!!");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decrypt_ValidBase64ButWrongCiphertext_ThrowsCryptographicException()
    {
        var service = CreateService();
        var randomBytes = Convert.ToBase64String(new byte[256]);

        var act = () => service.Decrypt(randomBytes);

        act.Should().Throw<CryptographicException>();
    }

}
