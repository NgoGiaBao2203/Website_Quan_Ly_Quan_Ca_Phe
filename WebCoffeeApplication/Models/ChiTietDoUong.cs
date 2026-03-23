using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCoffeeApplication.Models
{
    [Table("CHITIETDOUONG")]
    public class ChiTietDoUong
    {
        [Key]
        [Required]
        public int MaChiTietDoUong { get; set; }

        [MaxLength(5)]
        public string? KichCo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiaBan { get; set; }

        public int? MaDoUong { get; set; }

        [ForeignKey(nameof(MaDoUong))]
        public virtual DoUong? DoUong { get; set; }
    }
}
