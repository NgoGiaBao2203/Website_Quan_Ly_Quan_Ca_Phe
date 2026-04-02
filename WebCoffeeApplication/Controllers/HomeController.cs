using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Diagnostics;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
    public class HomeController(ILogger<HomeController> logger, UserManager<TaiKhoan> userManager) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly UserManager<TaiKhoan> _userManager = userManager;

        // [Authorize]: Chốt chặn bảo mật, chỉ cho phép người dùng đã đăng nhập vào trang chủ
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Bước 1: Trích xuất ID (NameIdentifier) của người dùng từ Cookie (Claims)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Bước 2: Kiểm tra và chuyển đổi ID từ kiểu chuỗi sang kiểu số nguyên (int)
            if (int.TryParse(userIdStr, out int userId))
            {
                // Bước 3: Truy vấn thông tin tài khoản chi tiết từ Database dựa vào ID đã lấy
                var currentUser = await _userManager.FindByIdAsync(userId.ToString());

                // Bước 4: Nếu tìm thấy người dùng, bắt đầu đóng gói dữ liệu để gửi ra View
                if (currentUser != null)
                {
                    // Lấy Họ Tên để hiển thị, nếu không có thì lấy tạm UserName
                    ViewData["CurrentUserFullName"] = string.IsNullOrWhiteSpace(currentUser.HoTen) ? currentUser.UserName : currentUser.HoTen;

                    ViewData["CurrentUserUserName"] = currentUser.UserName;
                    ViewData["CurrentUserEmail"] = currentUser.Email;

                    // Lấy số điện thoại từ cột tự tạo (SDT) hoặc PhoneNumber mặc định của hệ thống
                    ViewData["CurrentUserPhone"] = currentUser.SDT ?? currentUser.PhoneNumber ?? "-";
                }
            }

            // Bước 5: Trả về giao diện Index kèm theo các dữ liệu trong túi ViewData
            return View();
        }

        // [Authorize(Roles = "ChuQuan")]: Phân quyền đặc biệt, chỉ dành cho cấp quản lý cao nhất
        [Authorize(Roles = "ChuQuan")]
        public IActionResult Privacy()
        {
            // Trả về trang thông tin riêng tư/nhạy cảm của hệ thống
            return View();
        }

        // Cấu hình không lưu Cache để trang báo lỗi luôn hiển thị dữ liệu sự cố mới nhất
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Bước 1: Khởi tạo Model lỗi và lấy mã định danh RequestId để tiện tra cứu Log sau này
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}