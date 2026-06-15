using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoriiCoffee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProviderOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Refunds_StripeRefundId",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookEvents_StripeEventId",
                table: "PaymentWebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_Payments_StripePaymentIntentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_StripeSessionId",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Refunds",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "PaymentWebhookEvents",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_Provider_StripeRefundId",
                table: "Refunds",
                columns: new[] { "Provider", "StripeRefundId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_Provider_StripeEventId",
                table: "PaymentWebhookEvents",
                columns: new[] { "Provider", "StripeEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_StripePaymentIntentId",
                table: "Payments",
                columns: new[] { "Provider", "StripePaymentIntentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_StripeSessionId",
                table: "Payments",
                columns: new[] { "Provider", "StripeSessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Refunds_Provider_StripeRefundId",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookEvents_Provider_StripeEventId",
                table: "PaymentWebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_StripePaymentIntentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_StripeSessionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "PaymentWebhookEvents");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_StripeRefundId",
                table: "Refunds",
                column: "StripeRefundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_StripeEventId",
                table: "PaymentWebhookEvents",
                column: "StripeEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StripePaymentIntentId",
                table: "Payments",
                column: "StripePaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StripeSessionId",
                table: "Payments",
                column: "StripeSessionId",
                unique: true);
        }
    }
}
