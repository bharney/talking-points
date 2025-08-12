using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace talking_points.Migrations
{
    /// <inheritdoc />
    public partial class UpdateArticleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "ArticleDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "ArticleDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "ArticleDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "ArticleDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlToImage",
                table: "ArticleDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                table: "ArticleDetails");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "ArticleDetails");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "ArticleDetails");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "ArticleDetails");

            migrationBuilder.DropColumn(
                name: "UrlToImage",
                table: "ArticleDetails");

        }
    }
}
