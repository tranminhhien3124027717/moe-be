using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsToInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_EnrollmentID",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillingDate",
                table: "Courses");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingDate",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingPeriodEnd",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingPeriodStart",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDue",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.Sql("""
                ALTER TABLE Courses DROP COLUMN PaymentDue;
            """);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDue",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BillingCycle",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingDate",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_EnrollmentID_BillingPeriodStart",
                table: "Invoices",
                columns: new[] { "EnrollmentID", "BillingPeriodStart" },
                unique: true,
                filter: "[BillingPeriodStart] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_EnrollmentID_BillingPeriodStart",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillingDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillingPeriodEnd",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillingPeriodStart",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentDue",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillingDate",
                table: "Courses");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Invoices",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.Sql("""
                ALTER TABLE Courses DROP COLUMN PaymentDue;
            """);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDue",
                table: "Courses",
                type: "datetime2",
                nullable: true);


            migrationBuilder.AlterColumn<int>(
                name: "PaymentType",
                table: "Courses",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDue",
                table: "Courses",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BillingCycle",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingDate",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_EnrollmentID",
                table: "Invoices",
                column: "EnrollmentID");
        }
    }
}
