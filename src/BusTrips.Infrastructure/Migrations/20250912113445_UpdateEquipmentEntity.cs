using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTrips.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEquipmentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeActiveDiscription",
                table: "Equipment",
                newName: "DeactivationReason");

            migrationBuilder.AddColumn<string>(
                name: "BusNumber",
                table: "Equipment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "Equipment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossVehicleWeight",
                table: "Equipment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "Equipment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IssuingProvince",
                table: "Equipment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "Equipment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "EquipmentDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentDocuments_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocuments_EquipmentId",
                table: "EquipmentDocuments",
                column: "EquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentDocuments");

            migrationBuilder.DropColumn(
                name: "BusNumber",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "GrossVehicleWeight",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "IssuingProvince",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "DeactivationReason",
                table: "Equipment",
                newName: "DeActiveDiscription");
        }
    }
}
