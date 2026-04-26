namespace MoriiCoffee.Domain.Shared.Constants;

/// <summary>
/// Centralized Redis key segments and key builders for MoriiCoffee cache entries.
/// </summary>
public static class CachedKeyConstants
{
    public const string CATEGORIES = "categories";
    public const string BANNERS = "banners";
    public const string PRODUCTS = "products";
    public const string CART = "cart";

    public static string All(string resource)
    {
        return $"{resource}:all";
    }

    public static string ById(string resource, Guid id)
    {
        return $"{resource}:{id}";
    }

    public static string AllCategories()
    {
        return All(CATEGORIES);
    }

    public static string CategoryById(Guid id)
    {
        return ById(CATEGORIES, id);
    }

    public static string AllBanners()
    {
        return All(BANNERS);
    }

    public static string BannerById(Guid id)
    {
        return ById(BANNERS, id);
    }

    public static string ProductById(Guid id)
    {
        return ById(PRODUCTS, id);
    }

    public static string CartByUser(Guid userId)
    {
        return ById(CART, userId);
    }

    public static string GuestCart(string sessionId)
    {
        return $"{CART}:guest:{sessionId}";
    }

    public static string EntityById<T>(Guid id)
    {
        return ById(ToResourceName<T>(), id);
    }

    public static string EntityCollection<T>()
    {
        return All(ToResourceName<T>());
    }

    private static string ToResourceName<T>()
    {
        return typeof(T).Name switch
        {
            "Category" => CATEGORIES,
            "Banner" => BANNERS,
            "Product" => PRODUCTS,
            _ => typeof(T).Name.ToLowerInvariant()
        };
    }
}
