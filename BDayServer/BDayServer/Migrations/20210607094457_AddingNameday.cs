using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class AddingNameday : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f205e88-d393-4a0e-8134-2e40d04cf47c");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ec6f929f-daee-48d2-b4f7-6457d8ed11b4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DayOfNameDay",
                table: "Persons",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "54cbce16-c891-4eec-9a5d-62aa6af7ffb2", "dfd269bc-9990-45ee-8a53-c937df5a0da7", "Viewer", "VIEWER" },
                    { "8b19056e-62ad-4774-8e01-8a1f9a8b653a", "8160311b-bc18-43bf-bbb2-40f15fb0cd75", "Administrator", "ADMINISTRATOR" }
                });

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "Id",
                keyValue: new Guid("3d490a70-94ce-4d15-9494-5248280c2ce3"),
                column: "DayOfNameDay",
                value: new DateTime(2021, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Persons",
                keyColumn: "Id",
                keyValue: new Guid("c9d4c053-49b6-410c-bc78-2d54a9991870"),
                column: "DayOfNameDay",
                value: new DateTime(2021, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "54cbce16-c891-4eec-9a5d-62aa6af7ffb2");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "8b19056e-62ad-4774-8e01-8a1f9a8b653a");

            migrationBuilder.DropColumn(
                name: "DayOfNameDay",
                table: "Persons");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "1f205e88-d393-4a0e-8134-2e40d04cf47c", "f9f4bed2-fddd-4d2f-94fc-cd4787da55d5", "Viewer", "VIEWER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "ec6f929f-daee-48d2-b4f7-6457d8ed11b4", "8c14dc09-1a0a-441a-a4b2-157f0cc377f1", "Administrator", "ADMINISTRATOR" });
        }
    }
}
