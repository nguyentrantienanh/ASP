using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TienAnhGold.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEndedAndEmployeeEndedToChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmployeeEnded",
                table: "Chats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UserEnded",
                table: "Chats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$0ZDM3Ow988uSrdACj7T18.WDHo.8FBe0znJCuo9UK6Z2RMH/jX6z.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeEnded",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "UserEnded",
                table: "Chats");

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$FzLC5o/FSZmo1Zc.i5Xny.oKyeCCerRnz2FSrd4gMUgjunkhlE5.i");
        }
    }
}
