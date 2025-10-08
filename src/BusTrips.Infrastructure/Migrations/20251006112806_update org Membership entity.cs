using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTrips.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateorgMembershipentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedAt",
                table: "OrganizationMemberships");

            migrationBuilder.AddColumn<bool>(
                name: "IsInvited",
                table: "OrganizationMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInvited",
                table: "OrganizationMemberships");

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedAt",
                table: "OrganizationMemberships",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
