using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TienAnhGold.Migrations
{
    /// <inheritdoc />
    public partial class AddHasAdminJoinedMessageToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAdminJoinedMessage",
                table: "Chats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$FzLC5o/FSZmo1Zc.i5Xny.oKyeCCerRnz2FSrd4gMUgjunkhlE5.i");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAdminJoinedMessage",
                table: "Chats");

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$S001euA4aK5JGA.7p9hdN.9A5vvRq2x7mJEJB9MV8FfNI9OsswvxO");
        }
    }
}
