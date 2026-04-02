using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCoffeeApplication.Models
{
    public enum TrangThaiHoaDon
    {
        HuyDonHang = 0,
        DangXuLy = 1,
        DaLenMon = 2,
        DaThanhToan = 3
    }

    [Table("HOADON")]
    public class HoaDon
    {
        [Key]
        [Required]
        public int MaHoaDon { get; set; }

        public DateTime? NgayTao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongTien { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái hóa đơn.")]
        [EnumDataType(typeof(TrangThaiHoaDon), ErrorMessage = "Trạng thái hóa đơn không hợp lệ.")]
        public TrangThaiHoaDon? TrangThai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số bàn.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số bàn phải lớn hơn 0.")]
        public int? SoBan { get; set; }

        public int TrangThaiXoa { get; set; }

        public int? MaTaiKhoan { get; set; }

        [ForeignKey(nameof(MaTaiKhoan))]
        public virtual TaiKhoan? TaiKhoan { get; set; }

        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
    }
}
