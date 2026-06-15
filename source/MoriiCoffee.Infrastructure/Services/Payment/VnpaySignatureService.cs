using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MoriiCoffee.Infrastructure.Services.Payment;

public sealed class VnpaySignatureService
{
    public string Canonicalize(IEnumerable<KeyValuePair<string, string?>> values)
    {
        return string.Join("&", values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value) &&
                           !pair.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                           !pair.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{Encode(pair.Key)}={Encode(pair.Value!)}"));
    }

    public string Sign(IEnumerable<KeyValuePair<string, string?>> values, string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(Canonicalize(values));
        var key = Encoding.UTF8.GetBytes(secret);
        return Convert.ToHexString(HMACSHA512.HashData(key, bytes)).ToLowerInvariant();
    }

    public bool Verify(IEnumerable<KeyValuePair<string, string?>> values, string suppliedHash, string secret)
    {
        if (string.IsNullOrWhiteSpace(suppliedHash))
            return false;

        var expected = Sign(values, secret);
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(suppliedHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string SignRaw(string value, string secret) =>
        Convert.ToHexString(HMACSHA512.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    // VNPAY signs application/x-www-form-urlencoded values, where spaces are encoded as "+".
    // Replacing "+" with "%20" produces a different HMAC and VNPAY rejects the URL with code 70.
    public static string Encode(string value) => WebUtility.UrlEncode(value);
}
