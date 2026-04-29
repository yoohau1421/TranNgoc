using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranNgoc.Migrations
{
    /// <inheritdoc />
    public partial class RenameisActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isActive",
                table: "CompareTemplates",
                newName: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "CompareTemplates",
                newName: "isActive");
        }
    }
}
