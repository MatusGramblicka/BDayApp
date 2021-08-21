using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class AdditionalUserFiledIsAdmin2Rename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0e37786c-d34e-4d5c-9835-584f6b1b00d6");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a55e9753-7b17-4919-ad4f-14b982a76084");

            migrationBuilder.RenameColumn(
                name: "isAdmin",
                table: "AspNetUsers",
                newName: "IsAdmin");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "1f205e88-d393-4a0e-8134-2e40d04cf47c", "f9f4bed2-fddd-4d2f-94fc-cd4787da55d5", "Viewer", "VIEWER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "ec6f929f-daee-48d2-b4f7-6457d8ed11b4", "8c14dc09-1a0a-441a-a4b2-157f0cc377f1", "Administrator", "ADMINISTRATOR" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f205e88-d393-4a0e-8134-2e40d04cf47c");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ec6f929f-daee-48d2-b4f7-6457d8ed11b4");

            migrationBuilder.RenameColumn(
                name: "IsAdmin",
                table: "AspNetUsers",
                newName: "isAdmin");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "0e37786c-d34e-4d5c-9835-584f6b1b00d6", "79513504-468e-4019-b688-4bd9dd591fcd", "Viewer", "VIEWER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "a55e9753-7b17-4919-ad4f-14b982a76084", "82e23cd8-c758-4469-95e5-3c817975180a", "Administrator", "ADMINISTRATOR" });
        }
    }
}
