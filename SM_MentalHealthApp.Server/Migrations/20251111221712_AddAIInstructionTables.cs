using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAIInstructionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIInstructionCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Context = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "HealthCheck")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIInstructionCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AIInstructions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIInstructions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIInstructions_AIInstructionCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AIInstructionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructionCategories_Context",
                table: "AIInstructionCategories",
                column: "Context");

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructionCategories_Context_IsActive_DisplayOrder",
                table: "AIInstructionCategories",
                columns: new[] { "Context", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructionCategories_DisplayOrder",
                table: "AIInstructionCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructionCategories_IsActive",
                table: "AIInstructionCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructions_CategoryId",
                table: "AIInstructions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructions_CategoryId_IsActive_DisplayOrder",
                table: "AIInstructions",
                columns: new[] { "CategoryId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructions_DisplayOrder",
                table: "AIInstructions",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_AIInstructions_IsActive",
                table: "AIInstructions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIInstructions");

            migrationBuilder.DropTable(
                name: "AIInstructionCategories");
        }
    }
}
