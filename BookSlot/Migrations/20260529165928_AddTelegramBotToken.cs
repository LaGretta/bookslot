using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSlot.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBotToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BotToken",
                table: "TelegramBotConnections",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotToken",
                table: "TelegramBotConnections");
        }
    }
}
