using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CabinetName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentIndexValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TextValue = table.Column<string>(type: "TEXT", nullable: true),
                    NumberValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    DateValue = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIndexValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentIndexValues_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    StorageKey = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentParts_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIndexValues_DocumentId_FieldName",
                table: "DocumentIndexValues",
                columns: new[] { "DocumentId", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentParts_DocumentId",
                table: "DocumentParts",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentParts_StorageKey",
                table: "DocumentParts",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CabinetName_StoredAt_Id",
                table: "Documents",
                columns: new[] { "CabinetName", "StoredAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentIndexValues");

            migrationBuilder.DropTable(
                name: "DocumentParts");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
