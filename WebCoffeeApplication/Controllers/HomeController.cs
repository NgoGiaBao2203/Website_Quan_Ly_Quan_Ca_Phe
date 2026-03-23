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

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var currentUser = await _userManager.FindByIdAsync(userId.ToString());
                if (currentUser != null)
                {
                    ViewData["CurrentUserFullName"] = string.IsNullOrWhiteSpace(currentUser.HoTen) ? currentUser.UserName : currentUser.HoTen;
                    ViewData["CurrentUserUserName"] = currentUser.UserName;
                    ViewData["CurrentUserEmail"] = currentUser.Email;
                    ViewData["CurrentUserPhone"] = currentUser.SDT ?? currentUser.PhoneNumber ?? "-";
                }
            }

            return View();
        }

        [Authorize(Roles = "ChuQuan")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
