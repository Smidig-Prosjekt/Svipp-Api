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

            // Sjekk om kolonnene allerede eksisterer før de legges til
            // Bruk LOWER() for case-insensitive sammenligning siden PostgreSQL kan lagre
            // tabellnavn i forskjellige cases avhengig av hvordan de ble opprettet.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_schema = 'public' 
                                   AND LOWER(table_name) = LOWER('Drivers') 
                                   AND LOWER(column_name) = LOWER('CurrentLatitude')) THEN
                        ALTER TABLE ""Drivers"" ADD COLUMN ""CurrentLatitude"" double precision;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_schema = 'public' 
                                   AND LOWER(table_name) = LOWER('Drivers') 
                                   AND LOWER(column_name) = LOWER('CurrentLongitude')) THEN
                        ALTER TABLE ""Drivers"" ADD COLUMN ""CurrentLongitude"" double precision;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_schema = 'public' 
                                   AND LOWER(table_name) = LOWER('Drivers') 
                                   AND LOWER(column_name) = LOWER('LastLocationUpdatedAt')) THEN
                        ALTER TABLE ""Drivers"" ADD COLUMN ""LastLocationUpdatedAt"" timestamp without time zone;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_schema = 'public' 
                                   AND LOWER(table_name) = LOWER('Customers') 
                                   AND LOWER(column_name) = LOWER('CurrentLatitude')) THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""CurrentLatitude"" double precision;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_schema = 'public' 
                                   AND LOWER(table_name) = LOWER('Customers') 
                                   AND LOWER(column_name) = LOWER('CurrentLongitude')) THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""CurrentLongitude"" double precision;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_schema = 'public' 
                                   AND LOWER(table_name) = LOWER('Customers') 
                                   AND LOWER(column_name) = LOWER('LastLocationUpdatedAt')) THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""LastLocationUpdatedAt"" timestamp without time zone;
                    END IF;
                END $$;
            ");

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
