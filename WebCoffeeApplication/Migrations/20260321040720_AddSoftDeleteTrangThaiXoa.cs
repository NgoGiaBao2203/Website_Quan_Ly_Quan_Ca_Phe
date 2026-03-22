using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebCoffeeApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteTrangThaiXoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "TAIKHOAN",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiXoa",
                table: "TAIKHOAN",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TrangThai",
                table: "HOADON",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SoBan",
                table: "HOADON",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiXoa",
                table: "HOADON",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiXoa",
                table: "DOUONG",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiXoa",
                table: "DANHMUCDOUONG",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThaiXoa",
                table: "TAIKHOAN");

            migrationBuilder.DropColumn(
                name: "TrangThaiXoa",
                table: "HOADON");

            migrationBuilder.DropColumn(
                name: "TrangThaiXoa",
                table: "DOUONG");

            migrationBuilder.DropColumn(
                name: "TrangThaiXoa",
                table: "DANHMUCDOUONG");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "TAIKHOAN",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TrangThai",
                table: "HOADON",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SoBan",
                table: "HOADON",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
