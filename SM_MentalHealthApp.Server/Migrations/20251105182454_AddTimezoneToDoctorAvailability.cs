using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_MentalHealthApp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTimezoneToDoctorAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "DoctorAvailabilities",
                type: "longtext",
                nullable: false,
                defaultValue: "UTC")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "DoctorAvailabilities");
        }
    }
}
