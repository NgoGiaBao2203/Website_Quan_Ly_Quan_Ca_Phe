using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
    public class AccountController(
        SignInManager<TaiKhoan> signInManager,
        UserManager<TaiKhoan> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        ILogger<AccountController> logger) : Controller
    {
        // Khởi tạo các dịch vụ quản lý tài khoản (Đăng nhập, Người dùng, Quyền, Nhật ký)
        private readonly SignInManager<TaiKhoan> _signInManager = signInManager;
        private readonly UserManager<TaiKhoan> _userManager = userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager = roleManager;
        private readonly ILogger<AccountController> _logger = logger;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Bước 1: Lưu lại đường dẫn cũ để sau khi login xong quay lại đúng trang đó
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            // Bước 1: Giữ lại link cũ để phục vụ việc chuyển hướng sau khi login
            ViewData["ReturnUrl"] = returnUrl;

            // Bước 2: Kiểm tra dữ liệu người dùng nhập vào có hợp lệ theo Model không
            if (ModelState.IsValid)
            {
                // Bước 3: Tìm tài khoản trong Database dựa trên tên đăng nhập
                var user = await _userManager.FindByNameAsync(model.UserName);

                // Bước 4: Kiểm tra tài khoản có bị "Xóa mềm" (TrangThaiXoa = 1) hay không
                if (user != null && user.TrangThaiXoa == 1)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị xóa. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }

                // Bước 5: Thực hiện xác thực mật khẩu và tạo Cookie phiên đăng nhập
                var result = await _signInManager.PasswordSignInAsync(
                    model.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                // Bước 6: Xử lý kết quả đăng nhập thành công hoặc thất bại
                if (result.Succeeded)
                {
                    // Ghi nhật ký vào hệ thống và điều hướng về trang đích an toàn
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    // Thông báo lỗi nếu sai thông tin đăng nhập
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Bước 1: Thực hiện lệnh xóa Cookie đăng nhập trên trình duyệt
            await _signInManager.SignOutAsync();
            // Bước 2: Ghi nhật ký thoát hệ thống
            _logger.LogInformation("User logged out.");
            // Bước 3: Điều hướng người dùng về lại trang chủ
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            // Hiển thị trang thông báo khi người dùng truy cập vào vùng không có quyền
            return View();
        }

        // Hàm kiểm tra bảo mật để tránh lỗi 'Open Redirect' (chuyển hướng sang trang web độc hại)
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            // Bước 1: Kiểm tra xem link đích có phải là link nội bộ trong website không
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                // Bước 2: Nếu link lạ hoặc rỗng thì mặc định quay về trang chủ
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}