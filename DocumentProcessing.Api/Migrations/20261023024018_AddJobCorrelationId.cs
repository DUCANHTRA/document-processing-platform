using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentProcessing.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCorrelationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "ProcessingJobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "ProcessingJobs");
        }
    }
}
