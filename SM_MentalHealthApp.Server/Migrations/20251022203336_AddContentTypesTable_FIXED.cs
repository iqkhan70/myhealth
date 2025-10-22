using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddContentTypesTable_FIXED : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the ContentTypes table first
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

            // Step 2: Insert the content types with specific IDs to match the enum values
            migrationBuilder.Sql(@"
                INSERT INTO ContentTypes (Id, Name, Description, Icon, IsActive, SortOrder, CreatedAt) VALUES
                (1, 'Document', 'General document files (PDF, DOC, TXT, etc.)', '📄', 1, 1, NOW()),
                (2, 'Image', 'Image files (JPG, PNG, GIF, etc.)', '🖼️', 1, 2, NOW()),
                (3, 'Video', 'Video files (MP4, AVI, MOV, etc.)', '🎥', 1, 3, NOW()),
                (4, 'Audio', 'Audio files (MP3, WAV, FLAC, etc.)', '🎵', 1, 4, NOW()),
                (5, 'Other', 'Other file types', '📁', 1, 5, NOW())
                ON DUPLICATE KEY UPDATE 
                    Description = VALUES(Description),
                    Icon = VALUES(Icon),
                    IsActive = VALUES(IsActive),
                    SortOrder = VALUES(SortOrder);
            ");

            // Step 3: Add the new ContentTypeModelId column (keeping the original Type column for now)
            migrationBuilder.AddColumn<int>(
                name: "ContentTypeModelId",
                table: "Contents",
                type: "int",
                nullable: false,
                defaultValue: 1); // Default to Document type

            // Step 4: Copy the Type values to ContentTypeModelId (they should match since we seeded with same IDs)
            migrationBuilder.Sql(@"
                UPDATE Contents 
                SET ContentTypeModelId = Type 
                WHERE Type IN (1, 2, 3, 4, 5);
            ");

            // Step 5: Create indexes
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

            migrationBuilder.CreateIndex(
                name: "IX_Contents_ContentTypeModelId",
                table: "Contents",
                column: "ContentTypeModelId");

            // Step 6: Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Contents_ContentTypes_ContentTypeModelId",
                table: "Contents",
                column: "ContentTypeModelId",
                principalTable: "ContentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Step 7: Now we can safely drop the old Type column
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Contents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add back the Type column
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Contents",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Step 2: Copy ContentTypeModelId back to Type
            migrationBuilder.Sql(@"
                UPDATE Contents 
                SET Type = ContentTypeModelId 
                WHERE ContentTypeModelId IN (1, 2, 3, 4, 5);
            ");

            // Step 3: Drop foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_Contents_ContentTypes_ContentTypeModelId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Contents_ContentTypeModelId",
                table: "Contents");

            // Step 4: Drop the ContentTypeModelId column
            migrationBuilder.DropColumn(
                name: "ContentTypeModelId",
                table: "Contents");

            // Step 5: Drop the ContentTypes table
            migrationBuilder.DropTable(
                name: "ContentTypes");
        }
    }
}
