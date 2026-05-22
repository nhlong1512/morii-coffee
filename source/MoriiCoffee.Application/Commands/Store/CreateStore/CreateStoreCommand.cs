using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Store.CreateStore;

/// <summary>Command to create a new store location with all 7 days of opening hours.</summary>
public class CreateStoreCommand : ICommand<StoreDto>
{
    public CreateStoreCommand(CreateStoreDto dto)
    {
        Name = dto.Name;
        Slug = dto.Slug;
        Address = dto.Address;
        District = dto.District;
        City = dto.City;
        Province = dto.Province;
        Latitude = dto.Latitude;
        Longitude = dto.Longitude;
        Phone = dto.Phone;
        Email = dto.Email;
        CoverImageUrl = dto.CoverImageUrl;
        IsActive = dto.IsActive;
        DisplayOrder = dto.DisplayOrder;
        OpeningHours = dto.OpeningHours;
    }

    /// <summary>Display name of the store.</summary>
    public string Name { get; }

    /// <summary>Optional URL slug; auto-generated if not provided.</summary>
    public string? Slug { get; }

    /// <summary>Street address.</summary>
    public string Address { get; }

    /// <summary>Optional district or ward.</summary>
    public string? District { get; }

    /// <summary>City where the store is located.</summary>
    public string City { get; }

    /// <summary>Optional province or region.</summary>
    public string? Province { get; }

    /// <summary>Geographic latitude.</summary>
    public double Latitude { get; }

    /// <summary>Geographic longitude.</summary>
    public double Longitude { get; }

    /// <summary>Customer-facing phone number.</summary>
    public string Phone { get; }

    /// <summary>Optional store email address.</summary>
    public string? Email { get; }

    /// <summary>Optional URL of the store's cover image.</summary>
    public string? CoverImageUrl { get; }

    /// <summary>Whether the store should be immediately visible on the public locator.</summary>
    public bool IsActive { get; }

    /// <summary>Sort position on the public page.</summary>
    public int DisplayOrder { get; }

    /// <summary>Opening hours for each day of the week (must be exactly 7 items).</summary>
    public List<CreateStoreOpeningHoursDto> OpeningHours { get; }
}
