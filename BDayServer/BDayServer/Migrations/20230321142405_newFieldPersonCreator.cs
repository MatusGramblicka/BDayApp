using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BDayServer.Migrations
{
    public partial class newFieldPersonCreator : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {       
            migrationBuilder.AddColumn<string>(
                name: "PersonCreator",
                table: "Persons",
                type: "varchar(50)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");            
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {   
            migrationBuilder.DropColumn(
                name: "PersonCreator",
                table: "Persons");           
        }
    }
}
