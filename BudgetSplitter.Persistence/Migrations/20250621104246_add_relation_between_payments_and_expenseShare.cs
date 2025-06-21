using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_relation_between_payments_and_expenseShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExpenseShareId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExpenseId",
                table: "Payments",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExpenseShareId",
                table: "Payments",
                column: "ExpenseShareId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ExpenseShares_ExpenseShareId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Expenses_ExpenseId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ExpenseId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ExpenseShareId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpenseId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpenseShareId",
                table: "Payments");
        }
    }
}
