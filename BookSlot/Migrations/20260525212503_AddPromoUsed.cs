using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSlot.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoUsed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PromoUsed",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromoUsed",
                table: "Subscriptions");
        }
    }
}
