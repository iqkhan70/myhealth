using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIgnoreFieldsToJournalEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Users_EnteredByUserId",
                table: "JournalEntries");

            migrationBuilder.AddColumn<DateTime>(
                name: "IgnoredAt",
                table: "JournalEntries",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IgnoredByDoctorId",
                table: "JournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIgnoredByDoctor",
                table: "JournalEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_IgnoredByDoctorId",
                table: "JournalEntries",
                column: "IgnoredByDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_EnteredByUserId",
                table: "JournalEntries",
                column: "EnteredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_IgnoredByDoctorId",
                table: "JournalEntries",
                column: "IgnoredByDoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Users_EnteredByUserId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Users_IgnoredByDoctorId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_IgnoredByDoctorId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "IgnoredAt",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "IgnoredByDoctorId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "IsIgnoredByDoctor",
                table: "JournalEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Users_EnteredByUserId",
                table: "JournalEntries",
                column: "EnteredByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
