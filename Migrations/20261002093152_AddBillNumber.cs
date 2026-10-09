using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHealthcare.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBillNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillNumber",
                table: "Bills",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
        WITH numbered_bills AS
        (
            SELECT
                "BillId",
                ROW_NUMBER() OVER (
                    ORDER BY "GeneratedDate", "BillId"
                ) AS bill_number
            FROM "Bills"
        )
        UPDATE "Bills" AS b
        SET "BillNumber" =
            'BILL-' || LPAD(
                numbered_bills.bill_number::text,
                6,
                '0'
            )
        FROM numbered_bills
        WHERE b."BillId" = numbered_bills."BillId";
        """);

            migrationBuilder.AlterColumn<string>(
                name: "BillNumber",
                table: "Bills",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BillNumber",
                table: "Bills",
                column: "BillNumber",
                unique: true);
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bills_BillNumber",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "BillNumber",
                table: "Bills");
        }
    }
}
