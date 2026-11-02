using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aaas.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Animaux_PhotoAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotosAnimaux",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    Cle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotosAnimaux", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotosAnimaux_Animaux_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animaux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotosAnimaux_AnimalId_Ordre",
                table: "PhotosAnimaux",
                columns: new[] { "AnimalId", "Ordre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotosAnimaux");
        }
    }
}
