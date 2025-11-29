using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class EncryptDateOfBirth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add the new encrypted column as nullable first
            migrationBuilder.AddColumn<string>(
                name: "DateOfBirthEncrypted",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Step 2: Copy existing DateOfBirth values to DateOfBirthEncrypted
            // Note: This will store them as plain text initially. 
            // You'll need to run a data migration script to encrypt them.
            // For now, we'll store them as ISO 8601 strings which can be encrypted later.
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET DateOfBirthEncrypted = DATE_FORMAT(DateOfBirth, '%Y-%m-%dT%H:%i:%s.000000')
                WHERE DateOfBirth IS NOT NULL AND DateOfBirth != '0001-01-01 00:00:00'
            ");

            // Step 3: Make DateOfBirthEncrypted required (set default for any null values)
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET DateOfBirthEncrypted = '0001-01-01T00:00:00.000000'
                WHERE DateOfBirthEncrypted IS NULL
            ");

            migrationBuilder.AlterColumn<string>(
                name: "DateOfBirthEncrypted",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Step 4: Drop the old DateOfBirth column
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirthEncrypted",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
