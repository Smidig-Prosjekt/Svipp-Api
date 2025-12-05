using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Svipp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToDriverAndCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Make",
                table: "Vehicles",
                newName: "Brand");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Customers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "Brand",
                table: "Vehicles",
                newName: "Make");
        }
    }
}
