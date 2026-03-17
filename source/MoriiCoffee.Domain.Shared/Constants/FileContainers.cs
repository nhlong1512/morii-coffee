namespace MoriiCoffee.Domain.Shared.Constants;

/// <summary>
/// Logical container (folder) name constants used as the first path segment inside an S3 bucket.
/// Public containers (products, categories, banners) are stored in the public S3 bucket and served
/// via CloudFront CDN. Private containers (users) are stored in the private S3 bucket and accessed
/// via short-lived presigned URLs.
/// </summary>
public static class FileContainers
{
    /// <summary>User profile assets (avatars, identity documents). Private bucket.</summary>
    public const string USERS = "users";

    /// <summary>Product thumbnail and gallery images. Public bucket.</summary>
    public const string PRODUCTS = "products";

    /// <summary>Category icon images. Public bucket.</summary>
    public const string CATEGORIES = "categories";

    /// <summary>Promotional banner images. Public bucket.</summary>
    public const string BANNERS = "banners";

    /// <summary>
    /// Returns <c>true</c> for containers that belong to the public S3 bucket (CDN-served).
    /// Returns <c>false</c> for private containers.
    /// </summary>
    public static bool IsPublicContainer(string container) =>
        container is PRODUCTS or CATEGORIES or BANNERS;
}
