using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class AdditionalUserFiledIsAdmin2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0f154bb3-981a-4cf7-a227-3f9f1081a3d7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "81fba3fe-1aa9-4fb1-80f1-7ae60e8cbaa6");

            migrationBuilder.AddColumn<bool>(
                name: "isAdmin",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "0e37786c-d34e-4d5c-9835-584f6b1b00d6", "79513504-468e-4019-b688-4bd9dd591fcd", "Viewer", "VIEWER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "a55e9753-7b17-4919-ad4f-14b982a76084", "82e23cd8-c758-4469-95e5-3c817975180a", "Administrator", "ADMINISTRATOR" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0e37786c-d34e-4d5c-9835-584f6b1b00d6");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a55e9753-7b17-4919-ad4f-14b982a76084");

            migrationBuilder.DropColumn(
                name: "isAdmin",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "81fba3fe-1aa9-4fb1-80f1-7ae60e8cbaa6", "9cb22c84-96c1-4694-bb7b-808198cdcfe1", "Viewer", "VIEWER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "0f154bb3-981a-4cf7-a227-3f9f1081a3d7", "6e381f16-d731-4069-b33d-06305c4838bc", "Administrator", "ADMINISTRATOR" });
        }
    }
}
