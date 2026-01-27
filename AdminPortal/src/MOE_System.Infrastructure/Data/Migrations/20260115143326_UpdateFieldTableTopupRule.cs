using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFieldTableTopupRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeCondition",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "BalanceCondition",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "EduLevelCond",
                table: "TopupRules");

            migrationBuilder.AddColumn<string>(
                name: "BatchFilterType",
                table: "TopupRules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "TopupRules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExecuted",
                table: "TopupRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "TopupRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxBalance",
                table: "TopupRules",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "TopupRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinBalance",
                table: "TopupRules",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidentialStatus",
                table: "TopupRules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledTime",
                table: "TopupRules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SchoolingStatus",
                table: "TopupRules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchFilterType",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "IsExecuted",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "MaxBalance",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "MinBalance",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "ResidentialStatus",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "ScheduledTime",
                table: "TopupRules");

            migrationBuilder.DropColumn(
                name: "SchoolingStatus",
                table: "TopupRules");

            migrationBuilder.AddColumn<string>(
                name: "AgeCondition",
                table: "TopupRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BalanceCondition",
                table: "TopupRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EduLevelCond",
                table: "TopupRules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
