using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentProcessing.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaxRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxRetries",
                table: "ProcessingJobs",
                newName: "MaxRetryCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxRetryCount",
                table: "ProcessingJobs",
                newName: "MaxRetries");
        }
    }
}
