using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCoffeeApplication.Models
{
  [Table("CHITIETHOADON")]
  public class ChiTietHoaDon
  {
    [Key]
    public int MaChiTietHoaDon { get; set; }

    public int? SoLuong { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DonGia { get; set; }

    [MaxLength(1000)]
    public string? GhiChu { get; set; }

    public int? MaHoaDon { get; set; }

    [ForeignKey(nameof(MaHoaDon))]
    public virtual HoaDon? HoaDon { get; set; }

    public int? MaDoUong { get; set; }

    [ForeignKey(nameof(MaDoUong))]
    public virtual DoUong? DoUong { get; set; }
  }
}
