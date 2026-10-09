using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentProcessing.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDocumentResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProcessingResults_DocumentId",
                table: "ProcessingResults",
                column: "DocumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessingResults_DocumentId",
                table: "ProcessingResults");
        }
    }
}
