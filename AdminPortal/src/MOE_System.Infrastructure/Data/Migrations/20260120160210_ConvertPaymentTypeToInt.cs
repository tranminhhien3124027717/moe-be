using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOE_System.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPaymentTypeToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert string values to int for Invoices table
            migrationBuilder.Sql(@"
                UPDATE Invoices 
                SET PaymentType = CASE 
                    WHEN PaymentType = 'OneTime' THEN '0'
                    WHEN PaymentType = 'Recurring' THEN '1'
                    ELSE PaymentType
                END
                WHERE PaymentType IN ('OneTime', 'Recurring')
            ");

            // Convert string values to int for Courses table
            migrationBuilder.Sql(@"
                UPDATE Courses 
                SET PaymentType = CASE 
                    WHEN PaymentType = 'OneTime' THEN '0'
                    WHEN PaymentType = 'Recurring' THEN '1'
                    ELSE PaymentType
                END
                WHERE PaymentType IN ('OneTime', 'Recurring')
            ");

            migrationBuilder.AlterColumn<int>(
                name: "PaymentType",
                table: "Invoices",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentType",
                table: "Courses",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
