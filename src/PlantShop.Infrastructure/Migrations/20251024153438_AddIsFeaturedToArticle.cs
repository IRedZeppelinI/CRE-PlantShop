using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFeaturedToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Articles");
        }
    }
}
