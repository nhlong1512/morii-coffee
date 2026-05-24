using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoriiCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGhnShippingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "UserDeliveryProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistrictName",
                table: "UserDeliveryProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvinceId",
                table: "UserDeliveryProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvinceName",
                table: "UserDeliveryProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WardCode",
                table: "UserDeliveryProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WardName",
                table: "UserDeliveryProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryInfo_DistrictId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryInfo_DistrictName",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryInfo_ProvinceId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryInfo_ProvinceName",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryInfo_WardCode",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryInfo_WardName",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryMethod",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShippingProvider",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingProviderEnvironment",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippingQuoteExpiresAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingQuoteFingerprint",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingServiceId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingServiceLabel",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingServiceTypeId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderEnvironment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClientOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShopId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: true),
                    CodAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FeeTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedDeliveryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrackingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReasonCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastRawDetailPayload = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    SignatureVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingResult = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingDistricts",
                columns: table => new
                {
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    ProvinceId = table.Column<int>(type: "integer", nullable: false),
                    DistrictName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupportType = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingDistricts", x => x.DistrictId);
                });

            migrationBuilder.CreateTable(
                name: "ShippingProvinces",
                columns: table => new
                {
                    ProvinceId = table.Column<int>(type: "integer", nullable: false),
                    ProvinceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingProvinces", x => x.ProvinceId);
                });

            migrationBuilder.CreateTable(
                name: "ShippingWards",
                columns: table => new
                {
                    WardCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    WardName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingWards", x => x.WardCode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ClientOrderCode",
                table: "Shipments",
                column: "ClientOrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ProviderOrderCode",
                table: "Shipments",
                column: "ProviderOrderCode");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status",
                table: "Shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentWebhookEvents_ClientOrderCode",
                table: "ShipmentWebhookEvents",
                column: "ClientOrderCode");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentWebhookEvents_EventType",
                table: "ShipmentWebhookEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentWebhookEvents_ProviderEventId",
                table: "ShipmentWebhookEvents",
                column: "ProviderEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentWebhookEvents_ProviderOrderCode",
                table: "ShipmentWebhookEvents",
                column: "ProviderOrderCode");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentWebhookEvents_ReceivedAt",
                table: "ShipmentWebhookEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDistricts_DistrictName",
                table: "ShippingDistricts",
                column: "DistrictName");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDistricts_ProvinceId",
                table: "ShippingDistricts",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingProvinces_ProvinceName",
                table: "ShippingProvinces",
                column: "ProvinceName");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingWards_DistrictId",
                table: "ShippingWards",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingWards_WardName",
                table: "ShippingWards",
                column: "WardName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropTable(
                name: "ShipmentWebhookEvents");

            migrationBuilder.DropTable(
                name: "ShippingDistricts");

            migrationBuilder.DropTable(
                name: "ShippingProvinces");

            migrationBuilder.DropTable(
                name: "ShippingWards");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "UserDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "DistrictName",
                table: "UserDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "UserDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "ProvinceName",
                table: "UserDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "UserDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "WardName",
                table: "UserDeliveryProfiles");

            migrationBuilder.DropColumn(
                name: "DeliveryInfo_DistrictId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryInfo_DistrictName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryInfo_ProvinceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryInfo_ProvinceName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryInfo_WardCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryInfo_WardName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingProvider",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingProviderEnvironment",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingQuoteExpiresAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingQuoteFingerprint",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingServiceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingServiceLabel",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingServiceTypeId",
                table: "Orders");
        }
    }
}
