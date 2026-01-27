using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStringFieldsToEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add temporary columns
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod_New",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status_New",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType_New",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status_New",
                table: "BatchExecutions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SchoolingStatus_New",
                table: "AccountHolders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EducationLevel_New",
                table: "AccountHolders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Step 2: Convert data - PaymentMethod (AccountBalance=0, CreditDebitCard=1, BankTransfer=2)
            migrationBuilder.Sql(@"
                UPDATE Transactions SET PaymentMethod_New = 
                    CASE 
                        WHEN PaymentMethod IN ('AccountBalance', 'SkillsFuture Credit') THEN 0
                        WHEN PaymentMethod IN ('CreditDebitCard', 'Credit Card') THEN 1
                        WHEN PaymentMethod IN ('BankTransfer', 'Bank Transfer') THEN 2
                        ELSE 0
                    END");

            // Step 3: Convert data - Invoice Status (Scheduled=0, Outstanding=1, FullyPaid=2)
            migrationBuilder.Sql(@"
                UPDATE Invoices SET Status_New = 
                    CASE 
                        WHEN Status IN ('Scheduled', 'Pending') THEN 0
                        WHEN Status = 'Outstanding' THEN 1
                        WHEN Status IN ('Paid', 'FullyPaid') THEN 2
                        ELSE 1
                    END");

            // Step 4: Convert data - PaymentType (OneTime=0, Recurring=1)
            migrationBuilder.Sql(@"
                UPDATE Courses SET PaymentType_New = 
                    CASE 
                        WHEN PaymentType IN ('OneTime', 'One-Time') THEN 0
                        WHEN PaymentType IN ('Recurring', 'Monthly') THEN 1
                        ELSE 0
                    END");

            // Step 5: Convert data - BatchExecution Status (Scheduled=0, Cancelled=1, Completed=2)
            migrationBuilder.Sql(@"
                UPDATE BatchExecutions SET Status_New = 
                    CASE 
                        WHEN Status IN ('SCHEDULED', 'Scheduled', 'Pending') THEN 0
                        WHEN Status IN ('Cancelled', 'CANCELLED') THEN 1
                        WHEN Status IN ('Completed', 'COMPLETED') THEN 2
                        ELSE 0
                    END");

            // Step 6: Convert data - SchoolingStatus (NotInSchool=0, InSchool=1)
            migrationBuilder.Sql(@"
                UPDATE AccountHolders SET SchoolingStatus_New = 
                    CASE 
                        WHEN SchoolingStatus IN ('Not In School', 'NotInSchool') THEN 0
                        WHEN SchoolingStatus IN ('In School', 'InSchool') THEN 1
                        ELSE 0
                    END");

            // Step 7: Convert data - EducationLevel (Primary=0, Secondary=1, PostSecondary=2, Tertiary=3, PostGraduate=4)
            migrationBuilder.Sql(@"
                UPDATE AccountHolders SET EducationLevel_New = 
                    CASE 
                        WHEN EducationLevel = 'Primary' THEN 0
                        WHEN EducationLevel = 'Secondary' THEN 1
                        WHEN EducationLevel IN ('PostSecondary', 'Post-Secondary', 'Post Secondary') THEN 2
                        WHEN EducationLevel = 'Tertiary' THEN 3
                        WHEN EducationLevel IN ('PostGraduate', 'Post-Graduate', 'Post Graduate') THEN 4
                        ELSE 0
                    END");

            // Step 8: Drop old columns
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "Transactions");
            migrationBuilder.DropColumn(name: "Status", table: "Invoices");
            migrationBuilder.DropColumn(name: "PaymentType", table: "Courses");
            migrationBuilder.DropColumn(name: "Status", table: "BatchExecutions");
            migrationBuilder.DropColumn(name: "SchoolingStatus", table: "AccountHolders");
            migrationBuilder.DropColumn(name: "EducationLevel", table: "AccountHolders");

            // Step 9: Rename new columns to original names
            migrationBuilder.RenameColumn(
                name: "PaymentMethod_New",
                table: "Transactions",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "Status_New",
                table: "Invoices",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PaymentType_New",
                table: "Courses",
                newName: "PaymentType");

            migrationBuilder.RenameColumn(
                name: "Status_New",
                table: "BatchExecutions",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "SchoolingStatus_New",
                table: "AccountHolders",
                newName: "SchoolingStatus");

            migrationBuilder.RenameColumn(
                name: "EducationLevel_New",
                table: "AccountHolders",
                newName: "EducationLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Transactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BatchExecutions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SchoolingStatus",
                table: "AccountHolders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EducationLevel",
                table: "AccountHolders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
