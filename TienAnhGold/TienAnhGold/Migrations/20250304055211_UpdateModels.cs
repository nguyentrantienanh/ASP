using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TienAnhGold.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$fQFwEDnWyx2b85/Ir4lmW.4qlE8NQQ7.HiQcSI0UXVmx6MT0tzvYi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$bQhq81wruj7ReTMr51ZmZuDmBjg/heUmbyyKZBQbC7fMDEZu9UMFu");
        }
    }
}
