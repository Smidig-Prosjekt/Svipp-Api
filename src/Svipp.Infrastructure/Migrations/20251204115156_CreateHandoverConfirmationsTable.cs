using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Svipp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationFieldsToCustomersAndDrivers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scooters_Locations_CurrentLocationId",
                table: "Scooters");

            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "Drivers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "Drivers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationUpdatedAt",
                table: "Drivers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "Customers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "Customers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationUpdatedAt",
                table: "Customers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Scooters_Locations_CurrentLocationId",
                table: "Scooters",
                column: "CurrentLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scooters_Locations_CurrentLocationId",
                table: "Scooters");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "LastLocationUpdatedAt",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastLocationUpdatedAt",
                table: "Customers");

            migrationBuilder.AddForeignKey(
                name: "FK_Scooters_Locations_CurrentLocationId",
                table: "Scooters",
                column: "CurrentLocationId",
                principalTable: "Locations",
                principalColumn: "LocationId");
        }
    }
}
