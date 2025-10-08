using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTrips.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class groupid_in_Trip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_GroupId",
                table: "Trips",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Groups_GroupId",
                table: "Trips",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Groups_GroupId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_GroupId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Trips");
        }
    }
}
