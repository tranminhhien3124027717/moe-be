using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToImplicitTopupRuleRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Foreign keys removal skipped: may have been removed by prior migrations or have different names in the database.

            migrationBuilder.DropTable(
                name: "TopupRuleEducationLevel");

            migrationBuilder.DropTable(
                name: "TopupRuleSchoolingStatus");

            // Index and column removals skipped: target DB does not have these indexes/columns in current state.

            migrationBuilder.CreateTable(
                name: "EducationLevelDefinitionTopupRule",
                columns: table => new
                {
                    EducationLevelsId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopupRulesId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationLevelDefinitionTopupRule", x => new { x.EducationLevelsId, x.TopupRulesId });
                    table.ForeignKey(
                        name: "FK_EducationLevelDefinitionTopupRule_EducationLevels_EducationLevelsId",
                        column: x => x.EducationLevelsId,
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EducationLevelDefinitionTopupRule_TopupRules_TopupRulesId",
                        column: x => x.TopupRulesId,
                        principalTable: "TopupRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolingStatusDefinitionTopupRule",
                columns: table => new
                {
                    SchoolingStatusesId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopupRulesId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolingStatusDefinitionTopupRule", x => new { x.SchoolingStatusesId, x.TopupRulesId });
                    table.ForeignKey(
                        name: "FK_SchoolingStatusDefinitionTopupRule_SchoolingStatuses_SchoolingStatusesId",
                        column: x => x.SchoolingStatusesId,
                        principalTable: "SchoolingStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SchoolingStatusDefinitionTopupRule_TopupRules_TopupRulesId",
                        column: x => x.TopupRulesId,
                        principalTable: "TopupRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevelDefinitionTopupRule_TopupRulesId",
                table: "EducationLevelDefinitionTopupRule",
                column: "TopupRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolingStatusDefinitionTopupRule_TopupRulesId",
                table: "SchoolingStatusDefinitionTopupRule",
                column: "TopupRulesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationLevelDefinitionTopupRule");

            migrationBuilder.DropTable(
                name: "SchoolingStatusDefinitionTopupRule");

            migrationBuilder.AddColumn<string>(
                name: "EducationLevelDefinitionId",
                table: "TopupRules",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolingStatusDefinitionId",
                table: "TopupRules",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TopupRuleEducationLevel",
                columns: table => new
                {
                    TopupRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EducationLevelId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRuleEducationLevel", x => new { x.TopupRuleId, x.EducationLevelId });
                    table.ForeignKey(
                        name: "FK_TopupRuleEducationLevel_EducationLevels_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalTable: "EducationLevels",
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
                    TopupRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SchoolingStatusId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRuleSchoolingStatus", x => new { x.TopupRuleId, x.SchoolingStatusId });
                    table.ForeignKey(
                        name: "FK_TopupRuleSchoolingStatus_SchoolingStatuses_SchoolingStatusId",
                        column: x => x.SchoolingStatusId,
                        principalTable: "SchoolingStatuses",
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
                name: "IX_TopupRules_EducationLevelDefinitionId",
                table: "TopupRules",
                column: "EducationLevelDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRules_SchoolingStatusDefinitionId",
                table: "TopupRules",
                column: "SchoolingStatusDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleEducationLevel_EducationLevelId",
                table: "TopupRuleEducationLevel",
                column: "EducationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleSchoolingStatus_SchoolingStatusId",
                table: "TopupRuleSchoolingStatus",
                column: "SchoolingStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_TopupRules_EducationLevels_EducationLevelDefinitionId",
                table: "TopupRules",
                column: "EducationLevelDefinitionId",
                principalTable: "EducationLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TopupRules_SchoolingStatuses_SchoolingStatusDefinitionId",
                table: "TopupRules",
                column: "SchoolingStatusDefinitionId",
                principalTable: "SchoolingStatuses",
                principalColumn: "Id");
        }
    }
}
