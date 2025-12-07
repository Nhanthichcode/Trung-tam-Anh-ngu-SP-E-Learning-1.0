using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ListeningResources_ListeningResourceId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "ListeningResourceId1",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadingPassageId1",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ListeningResourceId1",
                table: "Questions",
                column: "ListeningResourceId1");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ReadingPassageId1",
                table: "Questions",
                column: "ReadingPassageId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ListeningResources_ListeningResourceId",
                table: "Questions",
                column: "ListeningResourceId",
                principalTable: "ListeningResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ListeningResources_ListeningResourceId1",
                table: "Questions",
                column: "ListeningResourceId1",
                principalTable: "ListeningResources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions",
                column: "ReadingPassageId",
                principalTable: "ReadingPassages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId1",
                table: "Questions",
                column: "ReadingPassageId1",
                principalTable: "ReadingPassages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ListeningResources_ListeningResourceId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ListeningResources_ListeningResourceId1",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId1",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ListeningResourceId1",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ReadingPassageId1",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ListeningResourceId1",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ReadingPassageId1",
                table: "Questions");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ListeningResources_ListeningResourceId",
                table: "Questions",
                column: "ListeningResourceId",
                principalTable: "ListeningResources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions",
                column: "ReadingPassageId",
                principalTable: "ReadingPassages",
                principalColumn: "Id");
        }
    }
}
