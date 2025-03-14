using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TienAnhGold.Migrations
{
    /// <inheritdoc />
    public partial class CreateEmptyDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$qDDquA9k53U9ZrOQvfNfFed1lr8U/oaZNv4Hojkj1dJ40soJX2YGi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$70oxW.1idpVtW9t8kzxyvuiWzI7EUQjVMZTNnJOgZ9Klot8jRppmO");
        }
    }
}
