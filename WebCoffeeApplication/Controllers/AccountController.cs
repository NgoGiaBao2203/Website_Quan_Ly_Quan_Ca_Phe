using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
    public class AccountController(
        SignInManager<TaiKhoan> signInManager,
        UserManager<TaiKhoan> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        ILogger<AccountController> logger) : Controller
    {
        private readonly SignInManager<TaiKhoan> _signInManager = signInManager;
        private readonly UserManager<TaiKhoan> _userManager = userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager = roleManager;
        private readonly ILogger<AccountController> _logger = logger;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Chặn đăng nhập cho tài khoản đã bị xóa mềm.
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user != null && user.TrangThaiXoa == 1)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị xóa. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                else
                {
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
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}
