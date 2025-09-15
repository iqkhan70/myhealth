using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEnteredByUserIdToJournalEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnteredByUserId",
                table: "JournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EnteredByUserId",
                table: "JournalEntries",
                column: "EnteredByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_EnteredByUserId",
                table: "JournalEntries",
                column: "EnteredByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Users_EnteredByUserId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_EnteredByUserId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "EnteredByUserId",
                table: "JournalEntries");
        }
    }
}
