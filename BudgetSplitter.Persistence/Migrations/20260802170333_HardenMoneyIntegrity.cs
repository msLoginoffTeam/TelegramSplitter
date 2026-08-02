using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenMoneyIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ExpenseShares_ExpenseShareId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Expenses_ExpenseId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ExpenseShareId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseShares_ExpenseId",
                table: "ExpenseShares");

            migrationBuilder.DropColumn(
                name: "ExpenseShareId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "ExpenseShares");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_DifferentUsers",
                table: "Payments",
                sql: "\"FromUserId\" <> \"ToUserId\"");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseShares_ExpenseId_UserId",
                table: "ExpenseShares",
                columns: new[] { "ExpenseId", "UserId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExpenseShares_Amount_NonNegative",
                table: "ExpenseShares",
                sql: "\"Amount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Expenses_Title_NotBlank",
                table: "Expenses",
                sql: "length(btrim(\"Title\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Expenses_TotalAmount_Positive",
                table: "Expenses",
                sql: "\"TotalAmount\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Expenses_ExpenseId",
                table: "Payments",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Expenses_ExpenseId",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_DifferentUsers",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseShares_ExpenseId_UserId",
                table: "ExpenseShares");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExpenseShares_Amount_NonNegative",
                table: "ExpenseShares");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Expenses_Title_NotBlank",
                table: "Expenses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Expenses_TotalAmount_Positive",
                table: "Expenses");

            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseShareId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "ExpenseShares",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExpenseShareId",
                table: "Payments",
                column: "ExpenseShareId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseShares_ExpenseId",
                table: "ExpenseShares",
                column: "ExpenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ExpenseShares_ExpenseShareId",
                table: "Payments",
                column: "ExpenseShareId",
                principalTable: "ExpenseShares",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Expenses_ExpenseId",
                table: "Payments",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id");
        }
    }
}
