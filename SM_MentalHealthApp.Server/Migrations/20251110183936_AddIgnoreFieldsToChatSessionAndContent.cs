using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIgnoreFieldsToChatSessionAndContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IgnoredAt",
                table: "Contents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IgnoredByDoctorId",
                table: "Contents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIgnoredByDoctor",
                table: "Contents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IgnoredAt",
                table: "ChatSessions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IgnoredByDoctorId",
                table: "ChatSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIgnoredByDoctor",
                table: "ChatSessions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Contents_IgnoredByDoctorId",
                table: "Contents",
                column: "IgnoredByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_IgnoredByDoctorId",
                table: "ChatSessions",
                column: "IgnoredByDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Users_IgnoredByDoctorId",
                table: "ChatSessions",
                column: "IgnoredByDoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Contents_Users_IgnoredByDoctorId",
                table: "Contents",
                column: "IgnoredByDoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Users_IgnoredByDoctorId",
                table: "ChatSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Contents_Users_IgnoredByDoctorId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Contents_IgnoredByDoctorId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_IgnoredByDoctorId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "IgnoredAt",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "IgnoredByDoctorId",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "IsIgnoredByDoctor",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "IgnoredAt",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "IgnoredByDoctorId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "IsIgnoredByDoctor",
                table: "ChatSessions");
        }
    }
}
