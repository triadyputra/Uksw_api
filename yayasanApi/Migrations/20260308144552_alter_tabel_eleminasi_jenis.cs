using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace yayasanApi.Migrations
{
    /// <inheritdoc />
    public partial class alter_tabel_eleminasi_jenis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Jenis",
                table: "EleminasiKeuangan",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Jenis",
                table: "EleminasiKeuangan");
        }
    }
}
