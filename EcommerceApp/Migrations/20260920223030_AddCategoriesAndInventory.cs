using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            // 1. AGREGAR NUEVOS CAMPOS A PRODUCTS

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // 2. CREAR CATEGORÍAS

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "integer",
                        nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    Name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false),

                    Description = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true),

                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });
          
            // 3. INSERTAR LAS CUATRO CATEGORÍAS OFICIALES

            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("Name", "Description", "IsActive")
                VALUES
                    ('CinnamonsRolls', 'Rollos de canela y sus diferentes sabores.', TRUE),
                    ('Cookies', 'Galletas y cookies de Dulce Antojo.', TRUE),
                    ('Tortas por porcion', 'Tortas disponibles por porción.', TRUE),
                    ('Navidenos', 'Productos especiales de temporada navideña.', TRUE);
            """);

            // 4. CREAR INVENTARIOS

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "integer",
                        nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    ProductId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    CurrentQuantity = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    MinimumStock = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);

                    table.ForeignKey(
                        name: "FK_Inventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 5. PASAR LAS CATEGORÍAS ANTIGUAS A CategoryId

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "CategoryId" =
                    CASE
                        WHEN LOWER(TRIM("Category")) = 'cinnamonsrolls'
                            THEN (SELECT "Id" FROM "Categories"
                                  WHERE "Name" = 'CinnamonsRolls' LIMIT 1)

                        WHEN LOWER(TRIM("Category")) = 'cookies'
                            THEN (SELECT "Id" FROM "Categories"
                                  WHERE "Name" = 'Cookies' LIMIT 1)

                        WHEN LOWER(TRIM("Category")) = 'tortas por porcion'
                            THEN (SELECT "Id" FROM "Categories"
                                  WHERE "Name" = 'Tortas por porcion' LIMIT 1)

                        WHEN LOWER(TRIM("Category")) = 'navidenos'
                            THEN (SELECT "Id" FROM "Categories"
                                  WHERE "Name" = 'Navidenos' LIMIT 1)

                        ELSE NULL
                    END;
            """);

            // 6. SINCRONIZAR DISPONIBILIDAD CON EL STOCK EXISTENTE

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "IsAvailable" =
                    CASE
                        WHEN "Stock" > 0 AND "IsArchived" = FALSE
                            THEN TRUE
                        ELSE FALSE
                    END;
            """);

            // 7. CREAR INVENTARIO PARA LOS PRODUCTOS EXISTENTES

            migrationBuilder.Sql("""
                INSERT INTO "Inventories"
                    ("ProductId", "CurrentQuantity", "MinimumStock", "UpdatedAt")
                SELECT
                    "Id",
                    "Stock",
                    5,
                    CURRENT_TIMESTAMP
                FROM "Products";
            """);

            // 8. ÍNDICES

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories",
                column: "ProductId",
                unique: true);

            // 9. RELACIÓN PRODUCT -> CATEGORY

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Products");
        }
    }
}