using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeToNet10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Educations",
                keyColumn: "Id",
                keyValue: new Guid("c92ea179-dd5c-46ca-b7b5-b44a191b974c"));

            migrationBuilder.InsertData(
                table: "Educations",
                columns: new[] { "Id", "Degree", "Description", "FieldOfStudy", "School" },
                values: new object[] { new Guid("24b0ec6c-9aa0-424c-b651-ddeef59ed07a"), "Bachelor's degree", null, "Software engineering", "Sample university" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Educations",
                keyColumn: "Id",
                keyValue: new Guid("24b0ec6c-9aa0-424c-b651-ddeef59ed07a"));

            migrationBuilder.InsertData(
                table: "Educations",
                columns: new[] { "Id", "Degree", "Description", "FieldOfStudy", "School" },
                values: new object[] { new Guid("c92ea179-dd5c-46ca-b7b5-b44a191b974c"), "Bachelor's degree", null, "Software engineering", "Sample university" });
        }
    }
}
