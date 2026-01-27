using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBillingDateAndPaymentDueToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add temporary columns
            migrationBuilder.AddColumn<int>(
                name: "BillingDate_New",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDue_New",
                table: "Courses",
                type: "int",
                nullable: true);

            // Step 2: Set all temp columns to NULL (since we're changing the data type completely)
            migrationBuilder.Sql("UPDATE Courses SET BillingDate_New = NULL, PaymentDue_New = NULL");

            // Step 3: Drop old columns
            migrationBuilder.DropColumn(
                name: "BillingDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PaymentDue",
                table: "Courses");

            // Step 4: Rename new columns to original names
            migrationBuilder.RenameColumn(
                name: "BillingDate_New",
                table: "Courses",
                newName: "BillingDate");

            migrationBuilder.RenameColumn(
                name: "PaymentDue_New",
                table: "Courses",
                newName: "PaymentDue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: Add datetime columns
            migrationBuilder.AddColumn<DateTime>(
                name: "BillingDate_Old",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDue_Old",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            // Drop int columns
            migrationBuilder.DropColumn(
                name: "BillingDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PaymentDue",
                table: "Courses");

            // Rename back
            migrationBuilder.RenameColumn(
                name: "BillingDate_Old",
                table: "Courses",
                newName: "BillingDate");

            migrationBuilder.RenameColumn(
                name: "PaymentDue_Old",
                table: "Courses",
                newName: "PaymentDue");
        }
    }
}
