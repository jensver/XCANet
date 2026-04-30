using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XcaNet.Storage.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Templates",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "PrivateKeys",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "CertificateRevocationLists",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "CertificateRequests",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Certificates",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "PrivateKeys");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "CertificateRevocationLists");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Certificates");
        }
    }
}
