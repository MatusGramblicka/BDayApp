using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class EventHasOneUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventCreator",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Events",
                type: "varchar(95)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");           

            migrationBuilder.CreateIndex(
                name: "IX_Persons_UserId",
                table: "Persons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UserId",
                table: "Events",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_AspNetUsers_UserId",
                table: "Events",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_AspNetUsers_UserId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Persons_UserId",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Events_UserId",
                table: "Events");          

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "EventCreator",
                table: "Events",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");           
        }
    }
}
