using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigTopupRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TopupRules_EducationLevels_EducationLevelDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropIndex(
                name: "IX_TopupRules_EducationLevelDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "EducationLevelDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropForeignKey(
                name: "FK_TopupRules_SchoolingStatuses_SchoolingStatusDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropIndex(
                name: "IX_TopupRules_SchoolingStatusDefinitionId",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "SchoolingStatusDefinitionId",
                table: "TopupRules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_TopupRules_EducationLevelDefinitionId",
                table: "TopupRules",
                column: "EducationLevelDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRules_SchoolingStatusDefinitionId",
                table: "TopupRules",
                column: "SchoolingStatusDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TopupRules_EducationLevels_EducationLevelDefinitionId",
                table: "TopupRules",
                column: "EducationLevelDefinitionId",
                principalTable: "EducationLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TopupRules_SchoolingStatuses_SchoolingStatusDefinitionId",
                table: "TopupRules",
                column: "SchoolingStatusDefinitionId",
                principalTable: "SchoolingStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

    }
}
