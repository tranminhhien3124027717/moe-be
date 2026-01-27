using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TopupRuleSchoolingStatus",
                table: "TopupRuleSchoolingStatus");

            migrationBuilder.DropIndex(
                name: "IX_TopupRuleSchoolingStatus_TopupRuleId",
                table: "TopupRuleSchoolingStatus");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopupRuleEducationLevel",
                table: "TopupRuleEducationLevel");

            migrationBuilder.DropIndex(
                name: "IX_TopupRuleEducationLevel_TopupRuleId",
                table: "TopupRuleEducationLevel");

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopupRuleSchoolingStatus",
                table: "TopupRuleSchoolingStatus",
                columns: new[] { "TopupRuleId", "SchoolingStatusId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopupRuleEducationLevel",
                table: "TopupRuleEducationLevel",
                columns: new[] { "TopupRuleId", "EducationLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleSchoolingStatus_SchoolingStatusId",
                table: "TopupRuleSchoolingStatus",
                column: "SchoolingStatusId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TopupRules_EducationLevels_EducationLevelDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropForeignKey(
                name: "FK_TopupRules_SchoolingStatuses_SchoolingStatusDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopupRuleSchoolingStatus",
                table: "TopupRuleSchoolingStatus");

            migrationBuilder.DropIndex(
                name: "IX_TopupRuleSchoolingStatus_SchoolingStatusId",
                table: "TopupRuleSchoolingStatus");

            migrationBuilder.DropIndex(
                name: "IX_TopupRules_EducationLevelDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropIndex(
                name: "IX_TopupRules_SchoolingStatusDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TopupRuleEducationLevel",
                table: "TopupRuleEducationLevel");

            migrationBuilder.DropIndex(
                name: "IX_TopupRuleEducationLevel_EducationLevelId",
                table: "TopupRuleEducationLevel");

            migrationBuilder.DropColumn(
                name: "EducationLevelDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "SchoolingStatusDefinitionId",
                table: "TopupRules");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopupRuleSchoolingStatus",
                table: "TopupRuleSchoolingStatus",
                columns: new[] { "SchoolingStatusId", "TopupRuleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopupRuleEducationLevel",
                table: "TopupRuleEducationLevel",
                columns: new[] { "EducationLevelId", "TopupRuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleSchoolingStatus_TopupRuleId",
                table: "TopupRuleSchoolingStatus",
                column: "TopupRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRuleEducationLevel_TopupRuleId",
                table: "TopupRuleEducationLevel",
                column: "TopupRuleId");
        }
    }
}
