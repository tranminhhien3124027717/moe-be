using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionAndInternalRemarksToTopupRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TopupRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalRemarks",
                table: "TopupRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TopupRuleAccountHolders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopupRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountHolderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TopupAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRuleAccountHolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopupRuleAccountHolders_AccountHolders_AccountHolderId",
                        column: x => x.AccountHolderId,
                        principalTable: "AccountHolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopupRuleAccountHolders_TopupRules_TopupRuleId",
                        column: x => x.TopupRuleId,
                        principalTable: "TopupRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleAccountHolders_AccountHolderId",
                table: "TopupRuleAccountHolders",
                column: "AccountHolderId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleAccountHolders_TopupRuleId",
                table: "TopupRuleAccountHolders",
                column: "TopupRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopupRuleAccountHolders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "InternalRemarks",
                table: "TopupRules");
        }
    }
}
