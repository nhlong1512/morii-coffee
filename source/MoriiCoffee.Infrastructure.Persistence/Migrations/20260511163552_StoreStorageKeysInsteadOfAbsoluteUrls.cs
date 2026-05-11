using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoriiCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreStorageKeysInsteadOfAbsoluteUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Strip the CDN domain prefix from absolute CloudFront URLs, leaving only the S3 key.
            // e.g. "https://xxx.cloudfront.net/products/uuid/file.png" → "products/uuid/file.png"
            // CHARINDEX('/', url, 9) finds the first '/' after "https://" (position 9), i.e. the slash after the domain.
            // STUFF(url, 1, pos, '') removes the leading "https://domain" portion.
            // The WHERE clause targets only CloudFront URLs, leaving MinIO presigned URLs and nulls untouched.
            migrationBuilder.Sql(@"
                UPDATE Products
                SET ThumbnailUrl = STUFF(ThumbnailUrl, 1, CHARINDEX('/', ThumbnailUrl, 9), '')
                WHERE ThumbnailUrl LIKE 'https://%.cloudfront.net/%';

                UPDATE ProductImages
                SET Url = STUFF(Url, 1, CHARINDEX('/', Url, 9), '')
                WHERE Url LIKE 'https://%.cloudfront.net/%';

                UPDATE Banners
                SET ImageUrl = STUFF(ImageUrl, 1, CHARINDEX('/', ImageUrl, 9), '')
                WHERE ImageUrl LIKE 'https://%.cloudfront.net/%';

                UPDATE Categories
                SET IconUrl = STUFF(IconUrl, 1, CHARINDEX('/', IconUrl, 9), '')
                WHERE IconUrl LIKE 'https://%.cloudfront.net/%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration cannot restore the original CDN domain since it is stored in config, not in the DB.
            // To roll back: restore a database backup, or re-run the Up migration after updating CdnBaseUrl.
        }
    }
}
