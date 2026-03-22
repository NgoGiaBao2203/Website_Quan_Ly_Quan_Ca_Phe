using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebCoffeeApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddCoffeeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SDT",
                table: "TAIKHOAN",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11);

            migrationBuilder.AlterColumn<string>(
                name: "MatKhau",
                table: "TAIKHOAN",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateTable(
                name: "DANHMUCDOUONG",
                columns: table => new
                {
                    MaDanhMucDoUong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DANHMUCDOUONG", x => x.MaDanhMucDoUong);
                });

            migrationBuilder.CreateTable(
                name: "HOADON",
                columns: table => new
                {
                    MaHoaDon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: true),
                    SoBan = table.Column<int>(type: "int", nullable: true),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOADON", x => x.MaHoaDon);
                    table.ForeignKey(
                        name: "FK_HOADON_TAIKHOAN_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TAIKHOAN",
                        principalColumn: "MaTaiKhoan");
                });

            migrationBuilder.CreateTable(
                name: "DOUONG",
                columns: table => new
                {
                    MaDoUong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDoUong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LinkHinhAnh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: true),
                    MaDanhMucDoUong = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOUONG", x => x.MaDoUong);
                    table.ForeignKey(
                        name: "FK_DOUONG_DANHMUCDOUONG_MaDanhMucDoUong",
                        column: x => x.MaDanhMucDoUong,
                        principalTable: "DANHMUCDOUONG",
                        principalColumn: "MaDanhMucDoUong");
                });

            migrationBuilder.CreateTable(
                name: "CHITIETDOUONG",
                columns: table => new
                {
                    MaChiTietDoUong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KichCo = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaDoUong = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHITIETDOUONG", x => x.MaChiTietDoUong);
                    table.ForeignKey(
                        name: "FK_CHITIETDOUONG_DOUONG_MaDoUong",
                        column: x => x.MaDoUong,
                        principalTable: "DOUONG",
                        principalColumn: "MaDoUong");
                });

            migrationBuilder.CreateTable(
                name: "CHITIETHOADON",
                columns: table => new
                {
                    MaChiTietHoaDon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoLuong = table.Column<int>(type: "int", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaHoaDon = table.Column<int>(type: "int", nullable: true),
                    MaDoUong = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHITIETHOADON", x => x.MaChiTietHoaDon);
                    table.ForeignKey(
                        name: "FK_CHITIETHOADON_DOUONG_MaDoUong",
                        column: x => x.MaDoUong,
                        principalTable: "DOUONG",
                        principalColumn: "MaDoUong");
                    table.ForeignKey(
                        name: "FK_CHITIETHOADON_HOADON_MaHoaDon",
                        column: x => x.MaHoaDon,
                        principalTable: "HOADON",
                        principalColumn: "MaHoaDon");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHITIETDOUONG_MaDoUong",
                table: "CHITIETDOUONG",
                column: "MaDoUong");

            migrationBuilder.CreateIndex(
                name: "IX_CHITIETHOADON_MaDoUong",
                table: "CHITIETHOADON",
                column: "MaDoUong");

            migrationBuilder.CreateIndex(
                name: "IX_CHITIETHOADON_MaHoaDon",
                table: "CHITIETHOADON",
                column: "MaHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_DOUONG_MaDanhMucDoUong",
                table: "DOUONG",
                column: "MaDanhMucDoUong");

            migrationBuilder.CreateIndex(
                name: "IX_HOADON_MaTaiKhoan",
                table: "HOADON",
                column: "MaTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CHITIETDOUONG");

            migrationBuilder.DropTable(
                name: "CHITIETHOADON");

            migrationBuilder.DropTable(
                name: "DOUONG");

            migrationBuilder.DropTable(
                name: "HOADON");

            migrationBuilder.DropTable(
                name: "DANHMUCDOUONG");

            migrationBuilder.AlterColumn<string>(
                name: "SDT",
                table: "TAIKHOAN",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MatKhau",
                table: "TAIKHOAN",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
