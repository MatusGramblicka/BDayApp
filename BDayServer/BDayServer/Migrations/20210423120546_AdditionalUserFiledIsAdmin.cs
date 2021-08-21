using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class AdditionalUserFiledIsAdmin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5a5478a5-1bf3-41ec-a684-639c7a204293");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cd51bc6a-97c9-4621-b6b5-42a1f3461370");

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

        protected override void Down(MigrationBuilder migrationBuilder)
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
                values: new object[] { "cd51bc6a-97c9-4621-b6b5-42a1f3461370", "9043a174-a79f-485f-b6d4-84423d7cd611", "Viewer", "VIEWER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "5a5478a5-1bf3-41ec-a684-639c7a204293", "45483723-a296-45d9-91e4-c218d931703a", "Administrator", "ADMINISTRATOR" });
        }
    }
}
