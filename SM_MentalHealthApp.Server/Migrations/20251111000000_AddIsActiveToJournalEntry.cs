using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToJournalEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "JournalEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "JournalEntries");
        }
    }
}

