using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTrips.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update_in_orgMember_entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "OrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "IsCreator",
                table: "OrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "IsReadOnly",
                table: "OrganizationMemberships");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AddedAt",
                table: "OrganizationMemberships",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<int>(
                name: "MemberType",
                table: "OrganizationMemberships",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberType",
                table: "OrganizationMemberships");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AddedAt",
                table: "OrganizationMemberships",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "OrganizationMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCreator",
                table: "OrganizationMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadOnly",
                table: "OrganizationMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
