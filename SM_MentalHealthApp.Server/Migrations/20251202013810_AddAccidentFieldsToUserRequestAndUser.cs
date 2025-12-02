using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAccidentFieldsToUserRequestAndUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRequests_MobilePhone",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "MobilePhone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MobilePhone",
                table: "UserRequests");

            migrationBuilder.AddColumn<string>(
                name: "AccidentAddress",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccidentDate",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccidentDetails",
                table: "Users",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalNotes",
                table: "Users",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfAccident",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorsInformation",
                table: "Users",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LawyersInformation",
                table: "Users",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MobilePhoneEncrypted",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PoliceCaseNumber",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Race",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RoadConditions",
                table: "Users",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VehicleDetails",
                table: "Users",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AccidentAddress",
                table: "UserRequests",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccidentDate",
                table: "UserRequests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccidentDetails",
                table: "UserRequests",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalNotes",
                table: "UserRequests",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "UserRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfAccident",
                table: "UserRequests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorsInformation",
                table: "UserRequests",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LawyersInformation",
                table: "UserRequests",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MobilePhoneEncrypted",
                table: "UserRequests",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PoliceCaseNumber",
                table: "UserRequests",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Race",
                table: "UserRequests",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RoadConditions",
                table: "UserRequests",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VehicleDetails",
                table: "UserRequests",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccidentAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccidentDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccidentDetails",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdditionalNotes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfAccident",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DoctorsInformation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LawyersInformation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MobilePhoneEncrypted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PoliceCaseNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Race",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoadConditions",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VehicleDetails",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccidentAddress",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "AccidentDate",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "AccidentDetails",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "AdditionalNotes",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "DateOfAccident",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "DoctorsInformation",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "LawyersInformation",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "MobilePhoneEncrypted",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "PoliceCaseNumber",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "Race",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "RoadConditions",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "VehicleDetails",
                table: "UserRequests");

            migrationBuilder.AddColumn<string>(
                name: "MobilePhone",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MobilePhone",
                table: "UserRequests",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_MobilePhone",
                table: "UserRequests",
                column: "MobilePhone");
        }
    }
}
