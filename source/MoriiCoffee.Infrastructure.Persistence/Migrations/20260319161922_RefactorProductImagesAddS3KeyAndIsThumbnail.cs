using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoriiCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorProductImagesAddS3KeyAndIsThumbnail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltText",
                table: "ProductImages");

            migrationBuilder.RenameColumn(
                name: "IsMain",
                table: "ProductImages",
                newName: "IsThumbnail");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "ProductImages",
                newName: "Url");

            migrationBuilder.AddColumn<string>(
                name: "S3Key",
                table: "ProductImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "S3Key",
                table: "ProductImages");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "ProductImages",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "IsThumbnail",
                table: "ProductImages",
                newName: "IsMain");

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "ProductImages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
