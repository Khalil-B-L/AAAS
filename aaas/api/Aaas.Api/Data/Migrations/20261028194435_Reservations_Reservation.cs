using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aaas.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Reservations_Reservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreeLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpireLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Logement = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Jardin = table.Column<bool>(type: "bit", nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Animaux_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animaux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_AnimalId",
                table: "Reservations",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_AnimalId_EnAttente",
                table: "Reservations",
                column: "AnimalId",
                unique: true,
                filter: "[Statut] = 'EnAttente'");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");
        }
    }
}
