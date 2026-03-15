using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitFullNameToFirstLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the new columns (nullable temporarily so the UPDATE can populate them).
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // 2. Populate FirstName / LastName from existing FullName data.
            //    Splits on the first space: everything before → FirstName, everything after → LastName.
            //    If FullName has no space, the whole value goes to FirstName and LastName stays empty.
            migrationBuilder.Sql(@"
                UPDATE Users
                SET
                    FirstName = CASE
                                    WHEN CHARINDEX(' ', FullName) > 0
                                    THEN LEFT(FullName, CHARINDEX(' ', FullName) - 1)
                                    ELSE FullName
                                END,
                    LastName  = CASE
                                    WHEN CHARINDEX(' ', FullName) > 0
                                    THEN SUBSTRING(FullName, CHARINDEX(' ', FullName) + 1, LEN(FullName))
                                    ELSE ''
                                END;
            ");

            // 3. Now it is safe to drop the old column.
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }
    }
}
