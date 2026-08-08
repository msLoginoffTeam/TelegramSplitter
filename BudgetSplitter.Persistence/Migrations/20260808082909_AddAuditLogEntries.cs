using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorTelegramId = table.Column<long>(type: "bigint", nullable: true),
                    ActorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActorUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubjectType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityKeyJson = table.Column<string>(type: "text", nullable: false),
                    OldValuesJson = table.Column<string>(type: "text", nullable: true),
                    NewValuesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_GroupId_OccurredAtUtc",
                table: "AuditLogEntries",
                columns: new[] { "GroupId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_OccurredAtUtc",
                table: "AuditLogEntries",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries");
        }
    }
}
