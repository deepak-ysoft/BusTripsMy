using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTrips.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addgroupentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Contacts_PrimaryContactId",
                table: "Trips");


            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Trips_PrimaryContactId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_SecondaryContactId",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "ItineraryRichTextHtml",
                table: "Trips",
                newName: "TripDocsUrl");

            migrationBuilder.RenameColumn(
                name: "ItineraryDocumentUrl",
                table: "Trips",
                newName: "InvoiceLinkUrl");

            migrationBuilder.RenameColumn(
                name: "GroupName",
                table: "Organizations",
                newName: "OrgName");

            migrationBuilder.RenameIndex(
                name: "IX_Organizations_GroupName",
                table: "Organizations",
                newName: "IX_Organizations_OrgName");

            migrationBuilder.AddColumn<string>(
                name: "EstimateLinkUrl",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuotedPrice",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Discription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Organizations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GroupName",
                table: "Groups",
                column: "GroupName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrgId",
                table: "Groups",
                column: "OrgId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropColumn(
                name: "EstimateLinkUrl",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "QuotedPrice",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "TripDocsUrl",
                table: "Trips",
                newName: "ItineraryRichTextHtml");

            migrationBuilder.RenameColumn(
                name: "InvoiceLinkUrl",
                table: "Trips",
                newName: "ItineraryDocumentUrl");

            migrationBuilder.RenameColumn(
                name: "OrgName",
                table: "Organizations",
                newName: "GroupName");

            migrationBuilder.RenameIndex(
                name: "IX_Organizations_OrgName",
                table: "Organizations",
                newName: "IX_Organizations_GroupName");

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_PrimaryContactId",
                table: "Trips",
                column: "PrimaryContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_SecondaryContactId",
                table: "Trips",
                column: "SecondaryContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_OrganizationId",
                table: "Contacts",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Contacts_PrimaryContactId",
                table: "Trips",
                column: "PrimaryContactId",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Contacts_SecondaryContactId",
                table: "Trips",
                column: "SecondaryContactId",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
