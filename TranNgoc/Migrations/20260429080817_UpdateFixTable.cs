using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranNgoc.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFixTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CompareTemplateColumns_TemplateId",
                table: "CompareTemplateColumns",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CompareRuleConfigs_TemplateId",
                table: "CompareRuleConfigs",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CompareMasterData_TemplateId",
                table: "CompareMasterData",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompareMasterData_CompareTemplates_TemplateId",
                table: "CompareMasterData",
                column: "TemplateId",
                principalTable: "CompareTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompareRuleConfigs_CompareTemplates_TemplateId",
                table: "CompareRuleConfigs",
                column: "TemplateId",
                principalTable: "CompareTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompareTemplateColumns_CompareTemplates_TemplateId",
                table: "CompareTemplateColumns",
                column: "TemplateId",
                principalTable: "CompareTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompareMasterData_CompareTemplates_TemplateId",
                table: "CompareMasterData");

            migrationBuilder.DropForeignKey(
                name: "FK_CompareRuleConfigs_CompareTemplates_TemplateId",
                table: "CompareRuleConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_CompareTemplateColumns_CompareTemplates_TemplateId",
                table: "CompareTemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_CompareTemplateColumns_TemplateId",
                table: "CompareTemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_CompareRuleConfigs_TemplateId",
                table: "CompareRuleConfigs");

            migrationBuilder.DropIndex(
                name: "IX_CompareMasterData_TemplateId",
                table: "CompareMasterData");
        }
    }
}
