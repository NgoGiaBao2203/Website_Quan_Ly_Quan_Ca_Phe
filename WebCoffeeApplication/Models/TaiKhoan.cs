using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCoffeeApplication.Models
{
  [Table("TAIKHOAN")]
  public class TaiKhoan : IdentityUser<int>
  {
    [Key]
    [Column("MaTaiKhoan")]
    public override int Id { get; set; }

    [Required]
    [Column("HoTen")]
    [StringLength(100)]
    public required string HoTen { get; set; }

    [NotMapped]
    public string? MatKhau
    {
      get => PasswordHash;
      set => PasswordHash = value;
    }

    [Column("VaiTro")]
    public int VaiTro { get; set; }

    [Column("NgayTao")]
    public DateTime NgayTao { get; set; }

    [Column("NgayCapNhat")]
    public DateTime NgayCapNhat { get; set; }

    [Column("SDT")]
    [StringLength(11)]
    public string? SDT { get; set; }

    [Column("TrangThaiXoa")]
    public int TrangThaiXoa { get; set; }

    // Map Identity properties to your custom columns
    [Required]
    public override string? UserName { get; set; }

    public override string? Email { get; set; }
  }
}
