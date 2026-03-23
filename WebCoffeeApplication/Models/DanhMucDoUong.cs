using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCoffeeApplication.Models
{
    [Table("DANHMUCDOUONG")]
    public class DanhMucDoUong
    {
        [Key]
        public int MaDanhMucDoUong { get; set; }

        [MaxLength(100)]
        public string? TenDanhMuc { get; set; }

        public int TrangThaiXoa { get; set; }

        public virtual ICollection<DoUong> DoUongs { get; set; } = new List<DoUong>();
    }
}
