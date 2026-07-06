using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class SeedEnvironments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Environments",
                columns: new[] { "EnvironmentId", "Name" },
                values: new object[,]
                {
                    { 1, "Integration" },
                    { 2, "Testing" },
                    { 3, "Production" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Environments",
                keyColumn: "EnvironmentId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Environments",
                keyColumn: "EnvironmentId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Environments",
                keyColumn: "EnvironmentId",
                keyValue: 3);
        }
    }
}
