using Microsoft.EntityFrameworkCore.Migrations;

namespace StorageService.Api.Migrations
{
    public partial class AddPrimaryKeyToStorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddPrimaryKey(
                name: "PK_Storage",
                table: "Storage",
                column: "ProductID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Storage",
                table: "Storage");
        }
    }
}
