using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aaas.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Animaux_Animal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Animaux",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Espece = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Sexe = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Age = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Taille = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Vaccine = table.Column<bool>(type: "bit", nullable: false),
                    Sterilise = table.Column<bool>(type: "bit", nullable: false),
                    Puce = table.Column<bool>(type: "bit", nullable: false),
                    EntenteEnfants = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EntenteChiens = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EntenteChats = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreeLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AdopteLe = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animaux", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animaux_Statut_CreeLe",
                table: "Animaux",
                columns: new[] { "Statut", "CreeLe" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Animaux");
        }
    }
}
