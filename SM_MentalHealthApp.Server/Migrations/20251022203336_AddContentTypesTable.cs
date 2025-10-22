using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddContentTypesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.RenameColumn(
            //     name: "Type",
            //     table: "Contents",
            //     newName: "ContentTypeModelId");

            // migrationBuilder.AddColumn<int>(
            //     name: "ContentTypeId",
            //     table: "Contents",
            //     type: "int",
            //     nullable: false,
            //     defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_ContentTypeModelId",
                table: "Contents",
                column: "ContentTypeModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_IsActive",
                table: "ContentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_Name",
                table: "ContentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_SortOrder",
                table: "ContentTypes",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_Contents_ContentTypes_ContentTypeModelId",
                table: "Contents",
                column: "ContentTypeModelId",
                principalTable: "ContentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contents_ContentTypes_ContentTypeModelId",
                table: "Contents");

            migrationBuilder.DropTable(
                name: "ContentTypes");

            migrationBuilder.DropIndex(
                name: "IX_Contents_ContentTypeModelId",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "ContentTypeId",
                table: "Contents");

            migrationBuilder.RenameColumn(
                name: "ContentTypeModelId",
                table: "Contents",
                newName: "Type");
        }
    }
}
