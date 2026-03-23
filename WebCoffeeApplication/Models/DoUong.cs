using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCoffeeApplication.Models
{
    [Table("DOUONG")]
    public class DoUong
    {
        [Key]
        [Required]
        public int MaDoUong { get; set; }

        [MaxLength(100)]
        public string? TenDoUong { get; set; }

        [MaxLength(200)]
        public string? LinkHinhAnh { get; set; }

        public int? TrangThai { get; set; }

        public int TrangThaiXoa { get; set; }

        public int? MaDanhMucDoUong { get; set; }

        [ForeignKey(nameof(MaDanhMucDoUong))]
        public virtual DanhMucDoUong? DanhMucDoUong { get; set; }

        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
        public virtual ICollection<ChiTietDoUong> ChiTietDoUongs { get; set; } = new List<ChiTietDoUong>();
    }
}
