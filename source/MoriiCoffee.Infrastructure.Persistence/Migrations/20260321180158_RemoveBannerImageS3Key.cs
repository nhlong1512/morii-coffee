using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoriiCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBannerImageS3Key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageS3Key",
                table: "Banners");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageS3Key",
                table: "Banners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
