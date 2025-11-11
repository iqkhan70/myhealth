using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIgnoreFieldsToClinicalNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IgnoredAt",
                table: "ClinicalNotes",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IgnoredByDoctorId",
                table: "ClinicalNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIgnoredByDoctor",
                table: "ClinicalNotes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_IgnoredByDoctorId",
                table: "ClinicalNotes",
                column: "IgnoredByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_IsIgnoredByDoctor",
                table: "ClinicalNotes",
                column: "IsIgnoredByDoctor");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalNotes_Users_IgnoredByDoctorId",
                table: "ClinicalNotes",
                column: "IgnoredByDoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalNotes_Users_IgnoredByDoctorId",
                table: "ClinicalNotes");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNotes_IgnoredByDoctorId",
                table: "ClinicalNotes");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNotes_IsIgnoredByDoctor",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "IgnoredAt",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "IgnoredByDoctorId",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "IsIgnoredByDoctor",
                table: "ClinicalNotes");
        }
    }
}
