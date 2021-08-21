using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class DatabaseCreation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DayOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Persons",
                columns: new[] { "Id", "DayOfBirth", "Name", "Surname" },
                values: new object[] { new Guid("c9d4c053-49b6-410c-bc78-2d54a9991870"), new DateTime(1988, 5, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Matúš", "Gramblička" });

            migrationBuilder.InsertData(
                table: "Persons",
                columns: new[] { "Id", "DayOfBirth", "Name", "Surname" },
                values: new object[] { new Guid("3d490a70-94ce-4d15-9494-5248280c2ce3"), new DateTime(1992, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Diana", "Grambličková" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Persons");
        }
    }
}
