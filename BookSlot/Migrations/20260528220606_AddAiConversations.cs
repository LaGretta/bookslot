using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BookSlot.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    ExternalChatId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerContact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiConversations_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiConversationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderType = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiConversationMessages_AiConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AiConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerContact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentDrafts_AiConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AiConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentDrafts_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationMessages_ConversationId_CreatedAt",
                table: "AiConversationMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_BusinessId_Channel_ExternalChatId",
                table: "AiConversations",
                columns: new[] { "BusinessId", "Channel", "ExternalChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDrafts_ConversationId",
                table: "AppointmentDrafts",
                column: "ConversationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDrafts_ServiceId",
                table: "AppointmentDrafts",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiConversationMessages");

            migrationBuilder.DropTable(
                name: "AppointmentDrafts");

            migrationBuilder.DropTable(
                name: "AiConversations");
        }
    }
}
