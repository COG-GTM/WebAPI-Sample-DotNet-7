using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Educations",
                keyColumn: "Id",
                keyValue: new Guid("c92ea179-dd5c-46ca-b7b5-b44a191b974c"));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Educations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Educations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Educations",
                columns: new[] { "Id", "Degree", "Description", "EndDate", "FieldOfStudy", "School", "StartDate" },
                values: new object[] { new Guid("3ccac3d5-2c41-44cc-a5bf-329ed4dd792c"), "Bachelor's degree", null, null, "Software engineering", "Sample university", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Educations",
                keyColumn: "Id",
                keyValue: new Guid("3ccac3d5-2c41-44cc-a5bf-329ed4dd792c"));

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Educations");

            migrationBuilder.InsertData(
                table: "Educations",
                columns: new[] { "Id", "Degree", "Description", "FieldOfStudy", "School" },
                values: new object[] { new Guid("c92ea179-dd5c-46ca-b7b5-b44a191b974c"), "Bachelor's degree", null, "Software engineering", "Sample university" });
        }
    }
}
