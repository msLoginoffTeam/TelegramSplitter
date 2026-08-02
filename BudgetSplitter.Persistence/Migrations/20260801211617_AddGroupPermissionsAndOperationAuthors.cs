using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupPermissionsAndOperationAuthors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_CreatedById",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_CreatedById",
                table: "Groups");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "Expenses",
                newName: "PayerId");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_CreatedById",
                table: "Expenses",
                newName: "IX_Expenses_PayerId");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupMemberPermissions",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMemberPermissions", x => new { x.GroupId, x.UserId, x.Permission });
                    table.ForeignKey(
                        name: "FK_GroupMemberPermissions_UserGroups_UserId_GroupId",
                        columns: x => new { x.UserId, x.GroupId },
                        principalTable: "UserGroups",
                        principalColumns: new[] { "UserId", "GroupId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedByUserId",
                table: "Payments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OwnerId",
                table: "Groups",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CreatedByUserId",
                table: "Expenses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberPermissions_UserId_GroupId",
                table: "GroupMemberPermissions",
                columns: new[] { "UserId", "GroupId" });

            migrationBuilder.Sql("""
                UPDATE "Groups" SET "OwnerId" = "CreatedById";
                UPDATE "Expenses" SET "CreatedByUserId" = "PayerId";
                UPDATE "Payments" SET "CreatedByUserId" = "FromUserId";

                INSERT INTO "UserGroups" ("UserId", "GroupId")
                SELECT "CreatedById", "Id" FROM "Groups"
                ON CONFLICT ("UserId", "GroupId") DO NOTHING;

                INSERT INTO "GroupMemberPermissions" ("GroupId", "UserId", "Permission")
                SELECT g."Id", g."OwnerId", p."Permission"
                FROM "Groups" g
                CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10), (11), (12), (13), (14), (15), (16)) AS p("Permission")
                ON CONFLICT ("GroupId", "UserId", "Permission") DO NOTHING;

                INSERT INTO "GroupMemberPermissions" ("GroupId", "UserId", "Permission")
                SELECT ug."GroupId", ug."UserId", p."Permission"
                FROM "UserGroups" ug
                INNER JOIN "Groups" g ON g."Id" = ug."GroupId"
                CROSS JOIN (VALUES (1), (7), (8), (10), (12), (13), (15)) AS p("Permission")
                WHERE ug."UserId" <> g."OwnerId"
                ON CONFLICT ("GroupId", "UserId", "Permission") DO NOTHING;
                """);

            migrationBuilder.AlterColumn<Guid>(name: "CreatedByUserId", table: "Payments", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "OwnerId", table: "Groups", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "CreatedByUserId", table: "Expenses", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_CreatedByUserId",
                table: "Expenses",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_PayerId",
                table: "Expenses",
                column: "PayerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_CreatedById",
                table: "Groups",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_OwnerId",
                table: "Groups",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_CreatedByUserId",
                table: "Payments",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_CreatedByUserId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_PayerId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_CreatedById",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_OwnerId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_CreatedByUserId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "GroupMemberPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CreatedByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Groups_OwnerId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CreatedByUserId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Expenses");

            migrationBuilder.RenameColumn(
                name: "PayerId",
                table: "Expenses",
                newName: "CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_PayerId",
                table: "Expenses",
                newName: "IX_Expenses_CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_CreatedById",
                table: "Expenses",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_CreatedById",
                table: "Groups",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
