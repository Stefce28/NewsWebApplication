using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsWebApp.Migrations
{
    /// <inheritdoc />
    public partial class MigrationNumTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Articles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<decimal>(
                name: "ShockValue",
                table: "Articles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ShockValue",
                table: "Articles");
        }
    }
}
