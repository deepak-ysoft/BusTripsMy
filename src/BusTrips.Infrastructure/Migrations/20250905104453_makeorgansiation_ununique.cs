using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTrips.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class makeorgansiation_ununique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_OrgName",
                table: "Organizations");

            migrationBuilder.AlterColumn<string>(
                name: "OrgName",
                table: "Organizations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrgName",
                table: "Organizations",
                column: "OrgName",
                unique: true,
                filter: "[OrgName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_OrgName",
                table: "Organizations");

            migrationBuilder.AlterColumn<string>(
                name: "OrgName",
                table: "Organizations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrgName",
                table: "Organizations",
                column: "OrgName",
                unique: true);
        }
    }
}
