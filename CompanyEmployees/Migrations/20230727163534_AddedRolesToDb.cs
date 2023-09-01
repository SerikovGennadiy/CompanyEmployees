using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CompanyEmployees.Migrations
{
    /// <inheritdoc />
    public partial class AddedRolesToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "905df5ce-cb29-481c-9f8e-afb06abf3a34");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "aa86df32-eff0-4ed7-a4d1-6355183fbdcd");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "77d89037-3abf-4d81-b3c2-6eff60741e59", "172d2347-ca5d-4027-ae48-4beeacd30b88", "Manager", "MANAGER" },
                    { "a3b0b209-3774-4dc5-a132-f4d631617a7e", "5429231f-6c22-489f-ab0a-6a9033655be0", "Administrator", "ADMINISTRATOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "77d89037-3abf-4d81-b3c2-6eff60741e59");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a3b0b209-3774-4dc5-a132-f4d631617a7e");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "905df5ce-cb29-481c-9f8e-afb06abf3a34", "b1d0da3d-4f72-409f-b7a4-d019bf2fbd6a", "Manager", "MANAGER" },
                    { "aa86df32-eff0-4ed7-a4d1-6355183fbdcd", "8ee03e39-42c2-4ef4-b636-ab3ac17efed8", "Administrator", "ADMINISTRATOR" }
                });
        }
    }
}
