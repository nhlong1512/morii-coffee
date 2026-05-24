namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Sandbox-only GHN integration settings used by the backend shipping module.
/// </summary>
public class GhnSettings
{
    public const string SectionName = "Ghn";

    public string BaseUrl { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public int ShopId { get; set; }

    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = string.Empty;

    public int DefaultServiceTypeId { get; set; }

    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Development-only escape hatch for GHN sandbox SSL chains that cannot be validated by the local host.
    /// Never enable this in production.
    /// </summary>
    public bool SkipTlsCertificateValidation { get; set; }
}
