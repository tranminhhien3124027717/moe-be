using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableTopupTargetAndSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TopupRules_EducationAccounts_TargetEducationAccountId",
                table: "TopupRules");

            migrationBuilder.DropIndex(
                name: "IX_TopupRules_TargetEducationAccountId",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "TargetEducationAccountId",
                table: "TopupRules");

            migrationBuilder.CreateTable(
                name: "TopupExecutionSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopupRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EducationAccountId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupExecutionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopupExecutionSnapshots_EducationAccounts_EducationAccountId",
                        column: x => x.EducationAccountId,
                        principalTable: "EducationAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopupExecutionSnapshots_TopupRules_TopupRuleId",
                        column: x => x.TopupRuleId,
                        principalTable: "TopupRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TopupRuleTargets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopupRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EducationAccountId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRuleTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopupRuleTargets_EducationAccounts_EducationAccountId",
                        column: x => x.EducationAccountId,
                        principalTable: "EducationAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopupRuleTargets_TopupRules_TopupRuleId",
                        column: x => x.TopupRuleId,
                        principalTable: "TopupRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopupExecutionSnapshots_EducationAccountId",
                table: "TopupExecutionSnapshots",
                column: "EducationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupExecutionSnapshots_TopupRuleId_EducationAccountId",
                table: "TopupExecutionSnapshots",
                columns: new[] { "TopupRuleId", "EducationAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleTargets_EducationAccountId",
                table: "TopupRuleTargets",
                column: "EducationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleTargets_TopupRuleId_EducationAccountId",
                table: "TopupRuleTargets",
                columns: new[] { "TopupRuleId", "EducationAccountId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopupExecutionSnapshots");

            migrationBuilder.DropTable(
                name: "TopupRuleTargets");

            migrationBuilder.AddColumn<string>(
                name: "TargetEducationAccountId",
                table: "TopupRules",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopupRules_TargetEducationAccountId",
                table: "TopupRules",
                column: "TargetEducationAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_TopupRules_EducationAccounts_TargetEducationAccountId",
                table: "TopupRules",
                column: "TargetEducationAccountId",
                principalTable: "EducationAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
