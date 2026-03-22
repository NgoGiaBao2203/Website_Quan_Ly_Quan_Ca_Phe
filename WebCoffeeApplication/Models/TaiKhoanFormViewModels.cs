using System.ComponentModel.DataAnnotations;

namespace WebCoffeeApplication.Models
{
  public class TaiKhoanCreateViewModel
  {
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    [Range(1, 2, ErrorMessage = "Vui lòng chọn vai trò hợp lệ.")]
    public int VaiTro { get; set; } = 2;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(11, ErrorMessage = "Số điện thoại không được vượt quá 11 ký tự.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Số điện thoại phải gồm 10-11 chữ số.")]
    public string SDT { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
  }

  public class TaiKhoanEditViewModel
  {
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    [Range(1, 2, ErrorMessage = "Vui lòng chọn vai trò hợp lệ.")]
    public int VaiTro { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(11, ErrorMessage = "Số điện thoại không được vượt quá 11 ký tự.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Số điện thoại phải gồm 10-11 chữ số.")]
    public string SDT { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Range(0, 1, ErrorMessage = "Trạng thái xóa không hợp lệ.")]
    public int TrangThaiXoa { get; set; }
  }
}
