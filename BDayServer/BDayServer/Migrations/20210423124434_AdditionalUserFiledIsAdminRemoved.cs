using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class AdditionalUserFiledIsAdminRemoved : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "6f1551f7-c5a7-4c2c-8d11-beabbe1f5c95");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "945e8118-ead4-4c0b-b5e1-f4d4957c17b8");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
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

        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "IsAdmin",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "945e8118-ead4-4c0b-b5e1-f4d4957c17b8", "7b39a1f6-f08e-42ba-8f93-f044c8d8b7cd", "Viewer", "VIEWER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "6f1551f7-c5a7-4c2c-8d11-beabbe1f5c95", "529b897f-fb0b-489f-9cc0-3d26c4b318c0", "Administrator", "ADMINISTRATOR" });
        }
    }
}
