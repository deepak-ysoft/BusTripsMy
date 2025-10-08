using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTrips.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update1InvitedOrganizationDtoentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupListItemVm");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InvitedOrganizationDto",
                table: "InvitedOrganizationDto");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "InvitedOrganizationDto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "InvitedOrganizationDto",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvitedOrganizationDto",
                table: "InvitedOrganizationDto",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "GroupListItemVm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvitedOrganizationDtoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCreator = table.Column<bool>(type: "bit", nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TripsCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupListItemVm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupListItemVm_InvitedOrganizationDto_InvitedOrganizationDtoId",
                        column: x => x.InvitedOrganizationDtoId,
                        principalTable: "InvitedOrganizationDto",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupListItemVm_InvitedOrganizationDtoId",
                table: "GroupListItemVm",
                column: "InvitedOrganizationDtoId");
        }
    }
}
