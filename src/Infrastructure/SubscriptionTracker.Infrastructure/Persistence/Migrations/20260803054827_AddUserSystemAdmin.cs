using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSystemAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAdmin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemAdmin",
                table: "Users");
        }
    }
}
