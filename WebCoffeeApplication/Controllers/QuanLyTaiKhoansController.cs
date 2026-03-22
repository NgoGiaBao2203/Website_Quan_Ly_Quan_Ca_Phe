using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebCoffeeApplication.DataContext;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
    [Authorize(Roles = "ChuQuan")]
    public class QuanLyTaiKhoansController(AppDataContext context, UserManager<TaiKhoan> userManager) : Controller
    {
        // DbContext dùng để truy vấn danh sách/chi tiết tài khoản cho màn hình quản trị.
        private readonly AppDataContext _context = context;
        // UserManager là đầu mối thao tác Identity (tạo/sửa/xóa user, role, validator...).
        private readonly UserManager<TaiKhoan> _userManager = userManager;

        // GET: QuanLyTaiKhoans
        /// <summary>
        /// Hiển thị danh sách tài khoản, sắp xếp theo thời điểm cập nhật gần nhất.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách tài khoản để hiển thị ở trang quản lý (bao gồm cả tài khoản đã xóa mềm).
            var danhSachTaiKhoan = await _context.TaiKhoan
                .OrderByDescending(x => x.NgayCapNhat)
                .ToListAsync();

            // Truyền ngữ cảnh bảo vệ quyền admin cho UI.
            ViewBag.CurrentUserId = GetCurrentUserId();
            ViewBag.LastChuQuanId = await GetSingleChuQuanIdAsync();

            return View(danhSachTaiKhoan);
        }

        // GET: QuanLyTaiKhoans/Details/5
        /// <summary>
        /// Hiển thị thông tin chi tiết của một tài khoản.
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            // Kiểm tra id đầu vào.
            if (id == null)
            {
                return NotFound();
            }

            // Truy vấn tài khoản theo id (bao gồm cả tài khoản đã xóa mềm).
            var taiKhoan = await _context.TaiKhoan
                .FirstOrDefaultAsync(m => m.Id == id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            // Truyền thêm thông tin Chủ quán cuối cùng để UI hiển thị đúng trạng thái bảo vệ.
            ViewBag.LastChuQuanId = await GetSingleChuQuanIdAsync();

            return View(taiKhoan);
        }

        // GET: QuanLyTaiKhoans/Create
        /// <summary>
        /// Trả về màn hình tạo tài khoản mới.
        /// </summary>
        public IActionResult Create()
        {
            return View(new TaiKhoanCreateViewModel());
        }

        // POST: QuanLyTaiKhoans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoanCreateViewModel model)
        {
            // Chuẩn hóa dữ liệu người dùng nhập để so sánh/lưu trữ nhất quán.
            model.HoTen = (model.HoTen ?? string.Empty).Trim();
            model.UserName = (model.UserName ?? string.Empty).Trim();
            model.SDT = (model.SDT ?? string.Empty).Trim();
            model.Email = (model.Email ?? string.Empty).Trim();

            // Dừng sớm nếu vi phạm DataAnnotations.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra trùng tên đăng nhập theo chuẩn normalize của Identity.
            var normalizedUserName = _userManager.NormalizeName(model.UserName);
            var userNameExists = await _userManager.Users
                .AnyAsync(x => x.NormalizedUserName == normalizedUserName);

            if (userNameExists)
            {
                ModelState.AddModelError(nameof(TaiKhoan.UserName), "Tên đăng nhập đã tồn tại.");
            }

            // Kiểm tra trùng email theo chuẩn normalize của Identity.
            var normalizedEmail = _userManager.NormalizeEmail(model.Email);
            var emailExists = await _userManager.Users
                .AnyAsync(x => x.NormalizedEmail == normalizedEmail);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(TaiKhoan.Email), "Email đã tồn tại.");
            }

            // Nếu có lỗi nghiệp vụ thì trả về lại form cùng thông báo.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ánh xạ ViewModel -> entity Identity user.
            var now = DateTime.Now;
            var taiKhoan = new TaiKhoan
            {
                HoTen = model.HoTen,
                VaiTro = model.VaiTro,
                SDT = model.SDT,
                UserName = model.UserName,
                Email = model.Email,
                NgayTao = now,
                NgayCapNhat = now
            };

            // Tạo user qua Identity để đảm bảo hash mật khẩu + validator chuẩn.
            var createResult = await _userManager.CreateAsync(taiKhoan, model.Password);
            if (!createResult.Succeeded)
            {
                AddIdentityErrorsToModelState(createResult);
                return View(model);
            }

            // Đồng bộ role Identity dựa trên cột VaiTro (1: ChuQuan, 2: NhanVien).
            var roleSyncResult = await SyncUserRoleAsync(taiKhoan, model.VaiTro);
            if (!roleSyncResult.Succeeded)
            {
                // Tránh tạo tài khoản mồ côi (đã tạo user nhưng chưa có role đúng).
                await _userManager.DeleteAsync(taiKhoan);
                AddIdentityErrorsToModelState(roleSyncResult);
                return View(model);
            }

            TempData["ToastMessage"] = "Thêm tài khoản thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        // GET: QuanLyTaiKhoans/Edit/5
        /// <summary>
        /// Hiển thị màn hình cập nhật tài khoản.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Lấy user từ Identity để đảm bảo dữ liệu đồng nhất.
            var taiKhoan = await _userManager.FindByIdAsync(id.Value.ToString());
            if (taiKhoan == null)
            {
                return NotFound();
            }

            // Ánh xạ entity -> ViewModel phục vụ hiển thị form.
            var model = new TaiKhoanEditViewModel
            {
                Id = taiKhoan.Id,
                HoTen = taiKhoan.HoTen,
                VaiTro = taiKhoan.VaiTro,
                SDT = taiKhoan.SDT ?? string.Empty,
                UserName = taiKhoan.UserName ?? string.Empty,
                Email = taiKhoan.Email ?? string.Empty,
                TrangThaiXoa = taiKhoan.TrangThaiXoa
            };

            // Xác định có cho phép hạ quyền xuống Nhân viên trên UI hay không.
            var currentUserId = GetCurrentUserId();
            var isSelfEdit = currentUserId.HasValue && currentUserId.Value == taiKhoan.Id;
            var isLastChuQuan = await IsLastChuQuanAsync(taiKhoan.Id);
            ViewBag.CanDemoteToNhanVien = !(isSelfEdit || isLastChuQuan);

            // Chỉ cho phép sửa TrangThaiXoa đối với tài khoản Nhân viên và không phải chính mình.
            ViewBag.CanEditTrangThaiXoa = taiKhoan.VaiTro == 2 && !isSelfEdit;

            return View(model);
        }

        // POST: QuanLyTaiKhoans/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaiKhoanEditViewModel model)
        {
            // Đảm bảo id trên route và form là cùng một bản ghi.
            if (id != model.Id)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            var isDemoteRequest = model.VaiTro != 1;

            // Chặn self-demote: Chủ quán không được tự đổi vai trò của chính mình xuống Nhân viên.
            if (isDemoteRequest && currentUserId.HasValue && currentUserId.Value == id)
            {
                ModelState.AddModelError(nameof(TaiKhoanEditViewModel.VaiTro), "Bạn không thể tự hạ quyền tài khoản đang đăng nhập.");
                TempData["ToastMessage"] = "Bạn không thể tự đổi vai trò của chính mình xuống Nhân viên.";
                TempData["ToastType"] = "warning";
            }

            // Chặn hạ quyền tài khoản Chủ quán cuối cùng để tránh mất hoàn toàn quyền quản trị.
            if (isDemoteRequest && await IsLastChuQuanAsync(id))
            {
                ModelState.AddModelError(nameof(TaiKhoanEditViewModel.VaiTro), "Không thể hạ quyền tài khoản Chủ quán cuối cùng.");
                TempData["ToastMessage"] = "Không thể hạ quyền tài khoản Chủ quán cuối cùng.";
                TempData["ToastType"] = "warning";
            }

            // Chuẩn hóa dữ liệu đầu vào trước khi validate nghiệp vụ.
            model.HoTen = (model.HoTen ?? string.Empty).Trim();
            model.UserName = (model.UserName ?? string.Empty).Trim();
            model.SDT = (model.SDT ?? string.Empty).Trim();
            model.Email = (model.Email ?? string.Empty).Trim();

            // Lấy user hiện tại từ Identity store.
            var taiKhoanDb = await _userManager.FindByIdAsync(id.ToString());
            if (taiKhoanDb == null)
            {
                return NotFound();
            }

            // Chặn sửa TrangThaiXoa nếu tài khoản là Chủ quán.
            if (model.TrangThaiXoa != taiKhoanDb.TrangThaiXoa && taiKhoanDb.VaiTro == 1)
            {
                ModelState.AddModelError(nameof(TaiKhoanEditViewModel.TrangThaiXoa),
                    "Không thể thay đổi trạng thái xóa của tài khoản Chủ quán.");
            }

            // Chặn tự sửa TrangThaiXoa của chính mình.
            if (model.TrangThaiXoa != taiKhoanDb.TrangThaiXoa && currentUserId.HasValue && currentUserId.Value == id)
            {
                ModelState.AddModelError(nameof(TaiKhoanEditViewModel.TrangThaiXoa),
                    "Bạn không thể thay đổi trạng thái xóa của chính tài khoản đang đăng nhập.");
            }

            // Chặn xóa mềm nếu tài khoản còn hóa đơn chưa thanh toán.
            if (model.TrangThaiXoa == 1 && taiKhoanDb.TrangThaiXoa != 1 && await HasUnpaidOrdersAsync(id))
            {
                ModelState.AddModelError(nameof(TaiKhoanEditViewModel.TrangThaiXoa),
                    "Không thể xóa tài khoản vì còn hóa đơn chưa thanh toán. Vui lòng thanh toán tất cả hóa đơn trước.");
            }

            // Dừng sớm nếu DataAnnotations chưa đạt.
            if (!ModelState.IsValid)
            {
                ViewBag.CanDemoteToNhanVien = !(currentUserId.HasValue && currentUserId.Value == id) && !await IsLastChuQuanAsync(id);
                ViewBag.CanEditTrangThaiXoa = taiKhoanDb.VaiTro == 2 && !(currentUserId.HasValue && currentUserId.Value == id);
                return View(model);
            }

            // Kiểm tra trùng username (loại trừ chính user đang sửa).
            var normalizedUserName = _userManager.NormalizeName(model.UserName);
            var userNameExists = await _userManager.Users
                .AnyAsync(x => x.NormalizedUserName == normalizedUserName && x.Id != id);
            if (userNameExists)
            {
                ModelState.AddModelError(nameof(TaiKhoan.UserName), "Tên đăng nhập đã tồn tại.");
            }

            // Kiểm tra trùng email (loại trừ chính user đang sửa).
            var normalizedEmail = _userManager.NormalizeEmail(model.Email);
            var emailExists = await _userManager.Users
                .AnyAsync(x => x.NormalizedEmail == normalizedEmail && x.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(TaiKhoan.Email), "Email đã tồn tại.");
            }

            // Trả về form khi còn lỗi nghiệp vụ.
            if (!ModelState.IsValid)
            {
                ViewBag.CanDemoteToNhanVien = !(currentUserId.HasValue && currentUserId.Value == id) && !await IsLastChuQuanAsync(id);
                ViewBag.CanEditTrangThaiXoa = taiKhoanDb.VaiTro == 2 && !(currentUserId.HasValue && currentUserId.Value == id);
                return View(model);
            }

            // Áp dụng thay đổi dữ liệu tài khoản.
            taiKhoanDb.HoTen = model.HoTen;
            taiKhoanDb.VaiTro = model.VaiTro;
            taiKhoanDb.SDT = model.SDT;
            taiKhoanDb.UserName = model.UserName;
            taiKhoanDb.Email = model.Email;
            taiKhoanDb.TrangThaiXoa = model.TrangThaiXoa;
            taiKhoanDb.NgayCapNhat = DateTime.Now;

            // Cập nhật user qua Identity để chạy đúng luồng validator/stamp.
            var updateResult = await _userManager.UpdateAsync(taiKhoanDb);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrorsToModelState(updateResult);
                ViewBag.CanDemoteToNhanVien = !(currentUserId.HasValue && currentUserId.Value == id) && !await IsLastChuQuanAsync(id);
                ViewBag.CanEditTrangThaiXoa = taiKhoanDb.VaiTro == 2 && !(currentUserId.HasValue && currentUserId.Value == id);
                return View(model);
            }

            // Đồng bộ lại role Identity theo VaiTro vừa thay đổi.
            var roleSyncResult = await SyncUserRoleAsync(taiKhoanDb, model.VaiTro);
            if (!roleSyncResult.Succeeded)
            {
                AddIdentityErrorsToModelState(roleSyncResult);
                ViewBag.CanDemoteToNhanVien = !(currentUserId.HasValue && currentUserId.Value == id) && !await IsLastChuQuanAsync(id);
                ViewBag.CanEditTrangThaiXoa = taiKhoanDb.VaiTro == 2 && !(currentUserId.HasValue && currentUserId.Value == id);
                return View(model);
            }

            TempData["ToastMessage"] = "Cập nhật tài khoản thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        // GET: QuanLyTaiKhoans/Delete/5
        /// <summary>
        /// Hiển thị màn hình xác nhận xóa tài khoản.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Chặn truy cập màn hình xóa nếu người dùng đang cố xóa chính tài khoản của mình.
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && currentUserId.Value == id.Value)
            {
                TempData["ToastMessage"] = "Bạn không thể tự xóa chính tài khoản đang đăng nhập.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            // Chặn xóa tài khoản Chủ quán cuối cùng để không làm mất toàn bộ quyền quản trị hệ thống.
            if (await IsLastChuQuanAsync(id.Value))
            {
                TempData["ToastMessage"] = "Không thể xóa tài khoản Chủ quán cuối cùng.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            // Chặn xóa tài khoản nếu còn hóa đơn chưa thanh toán.
            if (await HasUnpaidOrdersAsync(id.Value))
            {
                TempData["ToastMessage"] = "Không thể xóa tài khoản vì còn hóa đơn chưa thanh toán. Vui lòng thanh toán tất cả hóa đơn trước khi xóa.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = await _context.TaiKhoan
                .FirstOrDefaultAsync(m => m.Id == id && m.TrangThaiXoa != 1);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // POST: QuanLyTaiKhoans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Chặn tuyệt đối việc tự xóa tài khoản ở tầng server (kể cả khi gửi request thủ công).
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                TempData["ToastMessage"] = "Bạn không thể tự xóa chính tài khoản đang đăng nhập.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            // Chặn tuyệt đối việc xóa tài khoản Chủ quán cuối cùng ở tầng server.
            if (await IsLastChuQuanAsync(id))
            {
                TempData["ToastMessage"] = "Không thể xóa tài khoản Chủ quán cuối cùng.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            // Chặn tuyệt đối việc xóa tài khoản nếu còn hóa đơn chưa thanh toán ở tầng server.
            if (await HasUnpaidOrdersAsync(id))
            {
                TempData["ToastMessage"] = "Không thể xóa tài khoản vì còn hóa đơn chưa thanh toán. Vui lòng thanh toán tất cả hóa đơn trước khi xóa.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            // Lấy user theo id trước khi xóa.
            var taiKhoan = await _userManager.FindByIdAsync(id.ToString());
            if (taiKhoan == null || taiKhoan.TrangThaiXoa == 1)
            {
                TempData["ToastMessage"] = "Không tìm thấy tài khoản cần xóa.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            // Đánh dấu xóa mềm tài khoản.
            taiKhoan.TrangThaiXoa = 1;
            taiKhoan.NgayCapNhat = DateTime.Now;
            var updateResult = await _userManager.UpdateAsync(taiKhoan);
            if (!updateResult.Succeeded)
            {
                TempData["ToastMessage"] = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            TempData["ToastMessage"] = "Xóa tài khoản thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra tồn tại (giữ lại cho nhu cầu mở rộng/debug).
        private bool TaiKhoanExists(int id)
        {
            return _context.TaiKhoan.Any(e => e.Id == id);
        }

        // Trích xuất id user hiện tại từ Identity để dùng cho các kiểm tra phân quyền động.
        private int? GetCurrentUserId()
        {
            var userId = _userManager.GetUserId(User);
            if (int.TryParse(userId, out var parsedUserId))
            {
                return parsedUserId;
            }

            return null;
        }

        // Lấy id của Chủ quán duy nhất (nếu chỉ còn đúng 1 tài khoản thuộc role ChuQuan).
        private async Task<int?> GetSingleChuQuanIdAsync()
        {
            var chuQuanUsers = await _userManager.GetUsersInRoleAsync("ChuQuan");
            var activeChuQuanUsers = chuQuanUsers.Where(u => u.TrangThaiXoa != 1).ToList();
            if (activeChuQuanUsers.Count == 1)
            {
                return activeChuQuanUsers[0].Id;
            }

            return null;
        }

        // Kiểm tra tài khoản có phải là Chủ quán cuối cùng của hệ thống hay không.
        private async Task<bool> IsLastChuQuanAsync(int userId)
        {
            var singleChuQuanId = await GetSingleChuQuanIdAsync();
            return singleChuQuanId.HasValue && singleChuQuanId.Value == userId;
        }

        // Kiểm tra tài khoản có hóa đơn chưa thanh toán hay không.
        private async Task<bool> HasUnpaidOrdersAsync(int userId)
        {
            return await _context.HoaDon
                .AnyAsync(h => h.MaTaiKhoan == userId
                    && h.TrangThaiXoa != 1
                    && h.TrangThai != TrangThaiHoaDon.DaThanhToan);
        }

        // Đẩy lỗi từ IdentityResult lên ModelState để hiển thị trong form.
        private void AddIdentityErrorsToModelState(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // Đồng bộ vai trò Identity dựa trên cột VaiTro nghiệp vụ.
        private async Task<IdentityResult> SyncUserRoleAsync(TaiKhoan taiKhoan, int vaiTro)
        {
            var targetRole = vaiTro == 1 ? "ChuQuan" : "NhanVien";
            var managedRoles = new[] { "ChuQuan", "NhanVien" };

            // Lấy danh sách role hiện tại của user.
            var currentRoles = await _userManager.GetRolesAsync(taiKhoan);

            // Xác định các role cũ cần gỡ để tránh user mang đồng thời nhiều role nghiệp vụ.
            var rolesToRemove = currentRoles
                .Where(r => managedRoles.Contains(r) && !string.Equals(r, targetRole, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (rolesToRemove.Count > 0)
            {
                // Gỡ role không còn phù hợp.
                var removeResult = await _userManager.RemoveFromRolesAsync(taiKhoan, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return removeResult;
                }
            }

            if (!currentRoles.Contains(targetRole))
            {
                // Bổ sung role mục tiêu nếu chưa có.
                var addResult = await _userManager.AddToRoleAsync(taiKhoan, targetRole);
                if (!addResult.Succeeded)
                {
                    return addResult;
                }
            }

            return IdentityResult.Success;
        }
    }
}
