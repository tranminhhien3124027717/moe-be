using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTableSchoolingStatusAndEducationLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "SchoolingStatus",
                table: "TopupRules");

            migrationBuilder.CreateTable(
                name: "EducationLevel",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationLevel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolingStatus",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolingStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopupRuleEducationLevel",
                columns: table => new
                {
                    EducationLevelId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopupRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRuleEducationLevel", x => new { x.EducationLevelId, x.TopupRuleId });
                    table.ForeignKey(
                        name: "FK_TopupRuleEducationLevel_EducationLevel_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalTable: "EducationLevel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopupRuleEducationLevel_TopupRules_TopupRuleId",
                        column: x => x.TopupRuleId,
                        principalTable: "TopupRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopupRuleSchoolingStatus",
                columns: table => new
                {
                    SchoolingStatusId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopupRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRuleSchoolingStatus", x => new { x.SchoolingStatusId, x.TopupRuleId });
                    table.ForeignKey(
                        name: "FK_TopupRuleSchoolingStatus_SchoolingStatus_SchoolingStatusId",
                        column: x => x.SchoolingStatusId,
                        principalTable: "SchoolingStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopupRuleSchoolingStatus_TopupRules_TopupRuleId",
                        column: x => x.TopupRuleId,
                        principalTable: "TopupRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleEducationLevel_TopupRuleId",
                table: "TopupRuleEducationLevel",
                column: "TopupRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleSchoolingStatus_TopupRuleId",
                table: "TopupRuleSchoolingStatus",
                column: "TopupRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopupRuleEducationLevel");

            migrationBuilder.DropTable(
                name: "TopupRuleSchoolingStatus");

            migrationBuilder.DropTable(
                name: "EducationLevel");

            migrationBuilder.DropTable(
                name: "SchoolingStatus");

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "TopupRules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolingStatus",
                table: "TopupRules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
