using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XcaNet.Storage.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManagedCryptoArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PemData",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "PemData",
                table: "CertificateRequests");

            migrationBuilder.AddColumn<string>(
                name: "DataFormat",
                table: "Certificates",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "DerData",
                table: "Certificates",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsCertificateAuthority",
                table: "Certificates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KeyAlgorithm",
                table: "Certificates",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DataFormat",
                table: "CertificateRequests",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "DerData",
                table: "CertificateRequests",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "KeyAlgorithm",
                table: "CertificateRequests",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PrivateKeyId",
                table: "CertificateRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SubjectAlternativeNames",
                table: "CertificateRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataFormat",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "DerData",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "IsCertificateAuthority",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "KeyAlgorithm",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "DataFormat",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "DerData",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "KeyAlgorithm",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "PrivateKeyId",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "SubjectAlternativeNames",
                table: "CertificateRequests");

            migrationBuilder.AddColumn<string>(
                name: "PemData",
                table: "Certificates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PemData",
                table: "CertificateRequests",
                type: "TEXT",
                nullable: true);
        }
    }
}
