using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueYield_API.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurchaseCurrency",
                table: "Positions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseCurrency",
                table: "Positions");
        }
    }
}
