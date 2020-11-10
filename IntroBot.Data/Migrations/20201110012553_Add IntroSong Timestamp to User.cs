using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IntroBot.Data.Migrations
{
    public partial class AddIntroSongTimestamptoUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "IntroSongSeek",
                table: "ServerMembers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntroSongSeek",
                table: "ServerMembers");
        }
    }
}
