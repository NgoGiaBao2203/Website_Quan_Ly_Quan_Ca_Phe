using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebCoffeeApplication.DataContext;
using WebCoffeeApplication.Helpers;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
    [Authorize]
    public class ManageOrderController(AppDataContext context) : Controller
    {
        private readonly AppDataContext _context = context;

        /// <summary>
        /// Hiển thị danh sách hóa đơn, hỗ trợ tìm kiếm theo mã hóa đơn hoặc số bàn và phân trang
        /// </summary>
        public async Task<IActionResult> Index(string? searchString, int? pageNumber)
        {
            // Bước 1: Thiết lập số bản ghi trên mỗi trang.
            int pageSize = 5;

            // Bước 2: Khởi tạo truy vấn danh sách hóa đơn kèm thông tin liên quan.
            var orders = _context.HoaDon
                .Include(h => h.TaiKhoan)
                .Include(h => h.ChiTietHoaDons)
                .Where(h => h.TrangThaiXoa != 1)
                .OrderByDescending(h => h.NgayTao)
                .AsQueryable();

            // Bước 3: Nếu có từ khóa hợp lệ dạng số thì lọc theo mã hóa đơn hoặc số bàn.
            if (!string.IsNullOrEmpty(searchString))
            {
                if (int.TryParse(searchString, out int keyword))
                {
                    orders = orders.Where(h => h.MaHoaDon == keyword || h.SoBan == keyword);
                }
                else
                {
                    // Nếu nhập chữ báo lỗi định dạng 
                    TempData["ToastMessage"] = "Vui lòng nhập mã hóa đơn hoặc số bàn";
                    TempData["ToastType"] = "warning";
                    searchString = null;
                }
            }

            // Bước 4: Lưu lại từ khóa để hiển thị trên UI.
            ViewData["SearchString"] = searchString;

            // Bước 5: Đếm tổng kết quả sau lọc để xử lý thông báo tìm kiếm.
            var totalCount = await orders.CountAsync();

            // Bước 6: Không có kết quả -> báo cảnh báo và nạp lại danh sách đầy đủ.
            if (!string.IsNullOrEmpty(searchString) && totalCount == 0)
            {
                TempData["ToastMessage"] = "Không tìm thấy hóa đơn nào phù hợp với từ khóa tìm kiếm.";
                TempData["ToastType"] = "warning";

                orders = _context.HoaDon
                    .Include(h => h.TaiKhoan)
                    .Include(h => h.ChiTietHoaDons)
                    .Where(h => h.TrangThaiXoa != 1)
                    .OrderByDescending(h => h.NgayTao)
                    .AsQueryable();
                ViewData["SearchString"] = null;
            }
            // Bước 7: Có kết quả -> báo thành công.
            else if (!string.IsNullOrEmpty(searchString))
            {
                TempData["ToastMessage"] = "Đã tìm thấy hóa đơn phù hợp với từ khóa tìm kiếm.";
                TempData["ToastType"] = "success";
            }

            // Bước 8: Phân trang và trả về view.
            var result = await PaginatedList<HoaDon>.CreateAsync(orders, pageNumber ?? 1, pageSize);
            return View(result);
        }

        /// <summary>
        /// Hiển thị chi tiết một hóa đơn bao gồm các chi tiết đồ uống
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            // Bước 1: Kiểm tra id hợp lệ.
            if (id == null)
            {
                return NotFound();
            }

            // Bước 2: Tải hóa đơn cùng tài khoản và chi tiết món để hiển thị đầy đủ.
            var order = await _context.HoaDon
                .Include(h => h.TaiKhoan)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.DoUong)
                    .ThenInclude(d => d!.ChiTietDoUongs)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id && h.TrangThaiXoa != 1);

            // Bước 3: Không tìm thấy thì trả về 404.
            if (order == null)
            {
                return NotFound();
            }

            // Bước 4: Trả về trang chi tiết.
            return View(order);
        }

        /// <summary>
        /// Hiển thị giao diện form để tạo mới một hóa đơn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Bước 1: Nạp danh sách đồ uống cho dropdown.
            await PopulateDrinksViewBag();

            // Bước 2: Trả về model mặc định với trạng thái Đang xử lý.
            return View(new HoaDon { TrangThai = TrangThaiHoaDon.DangXuLy });
        }

        /// <summary>
        /// Xử lý dữ liệu được gửi từ form tạo mới hóa đơn
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HoaDon model, List<ChiTietHoaDon>? chiTietHoaDons)
        {
            // Bước 1: Loại bỏ các key điều hướng khỏi ModelState để tránh validate thừa.
            ModelState.Remove("TaiKhoan");
            ModelState.Remove("ChiTietHoaDons");
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("chiTietHoaDons")).ToList())
            {
                ModelState.Remove(key);
            }

            // Bước 2: Kiểm tra phải có ít nhất một dòng chi tiết món.
            if (chiTietHoaDons == null || chiTietHoaDons.Count == 0)
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất một món đồ uống vào hóa đơn.");
            }

            // Bước 3: Kiểm tra ràng buộc bàn đang bận.
            await ValidateTableAvailabilityForCreate(model);

            // Bước 3.1: Chặn thao tác tạo mới với trạng thái Đã thanh toán.
            if (model.TrangThai == TrangThaiHoaDon.DaThanhToan)
            {
                ModelState.AddModelError(nameof(model.TrangThai),
                "Không thể tạo mới hóa đơn ở trạng thái Đã thanh toán. Vui lòng dùng chức năng Thanh toán.");
            }

            // Bước 4: Nếu dữ liệu hợp lệ thì tạo hóa đơn và các dòng chi tiết.
            if (ModelState.IsValid)
            {
                // Bước 4.1: Gán thông tin thời gian và tổng tiền.
                model.NgayTao = DateTime.Now;
                model.TongTien = chiTietHoaDons?.Sum(ct => (ct.DonGia ?? 0) * (ct.SoLuong ?? 0));

                // Bước 4.2: Lấy người dùng hiện tại để gán nhân viên tạo đơn.
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    model.MaTaiKhoan = userId;
                }

                // Bước 4.3: Lưu hóa đơn cha trước để có MaHoaDon.
                model.ChiTietHoaDons.Clear();
                _context.HoaDon.Add(model);
                await _context.SaveChangesAsync();

                // Bước 4.4: Lưu từng dòng chi tiết món với MaHoaDon vừa tạo.
                if (chiTietHoaDons != null)
                {
                    foreach (var ct in chiTietHoaDons)
                    {
                        ct.MaHoaDon = model.MaHoaDon;
                        _context.ChiTietHoaDon.Add(ct);
                    }
                    await _context.SaveChangesAsync();
                }

                // Bước 4.5: Thông báo thành công và quay về danh sách.
                TempData["ToastMessage"] = "Tạo hóa đơn thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }

            // Bước 5: Nếu lỗi thì nạp lại dữ liệu dropdown và giữ nguyên danh sách món đã nhập.
            await PopulateDrinksViewBag();
            model.ChiTietHoaDons = chiTietHoaDons ?? new List<ChiTietHoaDon>();
            return View(model);
        }

        /// <summary>
        /// Hiển thị giao diện form để cập nhật hóa đơn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            // Bước 1: Kiểm tra id hợp lệ.
            if (id == null)
            {
                return NotFound();
            }

            // Bước 2: Tải hóa đơn và chi tiết món hiện có để hiển thị form sửa.
            var order = await _context.HoaDon
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.DoUong)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id && h.TrangThaiXoa != 1);

            // Bước 3: Không tìm thấy thì trả về 404.
            if (order == null)
            {
                return NotFound();
            }

            // Bước 4: Nạp dropdown đồ uống và trả về view.
            await PopulateDrinksViewBag();
            return View(order);
        }

        /// <summary>
        /// Xử lý dữ liệu được gửi từ form cập nhật hóa đơn
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HoaDon model, List<ChiTietHoaDon>? chiTietHoaDons)
        {
            // Bước 1: Đảm bảo id trên route khớp id trong model.
            if (id != model.MaHoaDon)
            {
                return NotFound();
            }

            // Bước 2: Loại bỏ các key điều hướng khỏi ModelState để tránh validate thừa.
            ModelState.Remove("TaiKhoan");
            ModelState.Remove("ChiTietHoaDons");
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("chiTietHoaDons")).ToList())
            {
                ModelState.Remove(key);
            }

            // Bước 3: Kiểm tra phải có ít nhất một dòng chi tiết món.
            if (chiTietHoaDons == null || chiTietHoaDons.Count == 0)
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất một món đồ uống vào hóa đơn.");
            }

            // Bước 4: Kiểm tra ràng buộc bàn đang bận (loại trừ chính hóa đơn đang sửa).
            await ValidateTableAvailabilityForEdit(model, id);

            // Bước 4.1: Chặn thao tác cập nhật trực tiếp sang trạng thái Đã thanh toán.
            if (model.TrangThai == TrangThaiHoaDon.DaThanhToan)
            {
                ModelState.AddModelError(nameof(model.TrangThai),
                "Không thể cập nhật trực tiếp sang trạng thái Đã thanh toán. Vui lòng dùng chức năng Thanh toán.");
            }

            // Bước 5: Nếu dữ liệu hợp lệ thì cập nhật hóa đơn và thay mới toàn bộ chi tiết món.
            if (ModelState.IsValid)
            {
                try
                {
                    // Bước 5.1: Lấy dữ liệu cũ để giữ lại thông tin không cho sửa.
                    var existing = await _context.HoaDon.AsNoTracking()
                        .FirstOrDefaultAsync(h => h.MaHoaDon == id && h.TrangThaiXoa != 1);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    // Bước 5.2: Giữ nguyên ngày tạo, nhân viên tạo và TrangThaiXoa ban đầu.
                    model.NgayTao = existing.NgayTao;
                    model.MaTaiKhoan = existing.MaTaiKhoan;
                    model.TrangThaiXoa = existing.TrangThaiXoa;

                    // Bước 5.3: Tính lại tổng tiền theo danh sách chi tiết mới.
                    model.TongTien = chiTietHoaDons?.Sum(ct => (ct.DonGia ?? 0) * (ct.SoLuong ?? 0));

                    // Bước 5.4: Cập nhật bản ghi hóa đơn cha.
                    model.ChiTietHoaDons.Clear();
                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    // Bước 5.5: Xóa toàn bộ chi tiết cũ.
                    var oldItems = await _context.ChiTietHoaDon
                        .Where(ct => ct.MaHoaDon == id).ToListAsync();
                    _context.ChiTietHoaDon.RemoveRange(oldItems);

                    // Bước 5.6: Thêm lại chi tiết mới từ form.
                    if (chiTietHoaDons != null)
                    {
                        foreach (var ct in chiTietHoaDons)
                        {
                            ct.MaChiTietHoaDon = 0;
                            ct.MaHoaDon = model.MaHoaDon;
                            _context.ChiTietHoaDon.Add(ct);
                        }
                    }

                    // Bước 5.7: Lưu thay đổi chi tiết.
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Bước 6: Nếu bị xung đột và bản ghi đã bị xóa thì trả về 404, ngược lại ném lỗi lên trên.
                    if (!await _context.HoaDon.AnyAsync(e => e.MaHoaDon == id))
                    {
                        return NotFound();
                    }
                    throw;
                }

                // Bước 7: Thông báo thành công và quay về danh sách.
                TempData["ToastMessage"] = "Cập nhật hóa đơn thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }

            // Bước 8: Nếu lỗi thì nạp lại dropdown và giữ nguyên danh sách món đã nhập.
            await PopulateDrinksViewBag();
            model.ChiTietHoaDons = chiTietHoaDons ?? new List<ChiTietHoaDon>();
            return View(model);
        }

        private async Task ValidateTableAvailabilityForCreate(HoaDon model)
        {
            // Bước 1: Bỏ qua nếu số bàn chưa hợp lệ; validate cơ bản sẽ xử lý phần này.
            if (!model.SoBan.HasValue || model.SoBan.Value < 1)
            {
                return;
            }

            // Bước 2: Tìm hóa đơn đang mở (chưa thanh toán/chưa hủy) của cùng bàn.
            var openOrder = await _context.HoaDon
                .AsNoTracking()
                .Where(h => h.TrangThaiXoa != 1
                    && h.SoBan == model.SoBan
                    && h.TrangThai != TrangThaiHoaDon.DaThanhToan
                    && h.TrangThai != TrangThaiHoaDon.HuyDonHang)
                .OrderByDescending(h => h.NgayTao)
                .FirstOrDefaultAsync();

            // Bước 3: Nếu tồn tại thì thêm lỗi để chặn tạo mới.
            if (openOrder != null)
            {
                ModelState.AddModelError(nameof(model.SoBan),
                    $"Bàn {model.SoBan} đang được giữ bởi hóa đơn #{openOrder.MaHoaDon}. Chỉ được tạo hóa đơn mới khi hóa đơn cũ đã thanh toán hoặc đã hủy.");
            }
        }

        private async Task ValidateTableAvailabilityForEdit(HoaDon model, int currentOrderId)
        {
            // Bước 1: Bỏ qua nếu số bàn chưa hợp lệ; validate cơ bản sẽ xử lý phần này.
            if (!model.SoBan.HasValue || model.SoBan.Value < 1)
            {
                return;
            }

            // Bước 2: Tìm hóa đơn đang mở của cùng bàn, loại trừ chính hóa đơn đang sửa.
            var openOrder = await _context.HoaDon
                .AsNoTracking()
                .Where(h => h.TrangThaiXoa != 1
                    && h.MaHoaDon != currentOrderId
                    && h.SoBan == model.SoBan
                    && h.TrangThai != TrangThaiHoaDon.DaThanhToan
                    && h.TrangThai != TrangThaiHoaDon.HuyDonHang)
                .OrderByDescending(h => h.NgayTao)
                .FirstOrDefaultAsync();

            // Bước 3: Nếu tồn tại thì thêm lỗi để chặn cập nhật gây trùng bàn đang mở.
            if (openOrder != null)
            {
                ModelState.AddModelError(nameof(model.SoBan),
                    $"Bàn {model.SoBan} đang được giữ bởi hóa đơn #{openOrder.MaHoaDon}. Vui lòng chọn bàn khác hoặc hoàn tất/hủy hóa đơn đang mở.");
            }
        }

        /// <summary>
        /// Hiển thị màn hình xác nhận thanh toán hóa đơn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Payment(int? id)
        {
            // Bước 1: Kiểm tra id hợp lệ.
            if (id == null)
            {
                return NotFound();
            }

            // Bước 2: Nạp hóa đơn và chi tiết món để hiển thị xác nhận thanh toán.
            var order = await _context.HoaDon
                .Include(h => h.TaiKhoan)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.DoUong)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id && h.TrangThaiXoa != 1);

            // Bước 3: Không tìm thấy hóa đơn thì trả về 404.
            if (order == null)
            {
                return NotFound();
            }

            // Bước 4: Nếu hóa đơn đã hủy thì không cho thanh toán.
            if (order.TrangThai == TrangThaiHoaDon.HuyDonHang)
            {
                TempData["ToastMessage"] = "Hóa đơn đã hủy, không thể thanh toán.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Details), new { id = order.MaHoaDon });
            }

            // Bước 5: Nếu đã thanh toán thì báo trạng thái và quay về chi tiết.
            if (order.TrangThai == TrangThaiHoaDon.DaThanhToan)
            {
                TempData["ToastMessage"] = "Hóa đơn này đã được thanh toán trước đó.";
                TempData["ToastType"] = "info";
                return RedirectToAction(nameof(Details), new { id = order.MaHoaDon });
            }

            // Bước 6: Trả về màn hình xác nhận thanh toán.
            return View(order);
        }

        /// <summary>
        /// Xác nhận thanh toán và cập nhật trạng thái hóa đơn thành Đã thanh toán
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(int id)
        {
            // Bước 1: Tìm hóa đơn cần thanh toán.
            var order = await _context.HoaDon.FirstOrDefaultAsync(h => h.MaHoaDon == id && h.TrangThaiXoa != 1);

            // Bước 2: Không tìm thấy hóa đơn thì trả về 404.
            if (order == null)
            {
                return NotFound();
            }

            // Bước 3: Không cho thanh toán nếu hóa đơn đã hủy.
            if (order.TrangThai == TrangThaiHoaDon.HuyDonHang)
            {
                TempData["ToastMessage"] = "Hóa đơn đã hủy, không thể thanh toán.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Bước 4: Nếu đã thanh toán thì không cập nhật lại.
            if (order.TrangThai == TrangThaiHoaDon.DaThanhToan)
            {
                TempData["ToastMessage"] = "Hóa đơn này đã được thanh toán trước đó.";
                TempData["ToastType"] = "info";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Bước 5: Cập nhật trạng thái cuối cùng thành Đã thanh toán.
            order.TrangThai = TrangThaiHoaDon.DaThanhToan;
            await _context.SaveChangesAsync();

            // Bước 6: Thông báo thành công và quay về trang chi tiết.
            TempData["ToastMessage"] = "Thanh toán hóa đơn thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa hóa đơn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            // Bước 1: Kiểm tra id hợp lệ.
            if (id == null)
            {
                return NotFound();
            }

            // Bước 2: Tải hóa đơn và dữ liệu liên quan để hiển thị xác nhận xóa.
            var order = await _context.HoaDon
                .Include(h => h.TaiKhoan)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.DoUong)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id && h.TrangThaiXoa != 1);

            // Bước 3: Không tìm thấy thì trả về 404.
            if (order == null)
            {
                return NotFound();
            }

            // Bước 4: Trả về trang xác nhận xóa.
            return View(order);
        }

        /// <summary>
        /// Tiến hành xóa vĩnh viễn hóa đơn và các chi tiết liên quan khỏi cơ sở dữ liệu
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Bước 1: Tìm hóa đơn cần xóa kèm chi tiết.
            var order = await _context.HoaDon
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.MaHoaDon == id);

            // Bước 2: Nếu tồn tại thì xóa chi tiết hóa đơn trước, sau đó xóa hóa đơn.
            if (order != null)
            {
                _context.ChiTietHoaDon.RemoveRange(order.ChiTietHoaDons);
                _context.HoaDon.Remove(order);
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Xóa hóa đơn thành công!";
                TempData["ToastType"] = "success";
            }
            // Bước 3: Nếu không tồn tại thì hiển thị thông báo phù hợp.
            else
            {
                TempData["ToastMessage"] = "Không tìm thấy hóa đơn cần xóa.";
                TempData["ToastType"] = "danger";
            }

            // Bước 4: Quay về danh sách.
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Chuẩn bị danh sách đồ uống cho dropdown trong form
        /// </summary>
        private async Task PopulateDrinksViewBag()
        {
            // Bước 1: Lấy các đồ uống đang hoạt động cùng danh sách kích cỡ/giá.
            var drinks = await _context.DoUong
                .Include(d => d.ChiTietDoUongs)
                .Where(d => d.TrangThai == 1 && d.TrangThaiXoa != 1)
                .ToListAsync();

            // Bước 2: Chuyển dữ liệu thành danh sách option cho dropdown.
            var drinkItems = drinks
                .SelectMany(d => d.ChiTietDoUongs.Select(ct => new
                {
                    Value = $"{d.MaDoUong}|{ct.GiaBan}",
                    Text = $"{d.TenDoUong} - {ct.KichCo} ({ct.GiaBan?.ToString("N0")}đ)",
                    MaDoUong = d.MaDoUong,
                    GiaBan = ct.GiaBan ?? 0
                }))
                .ToList();

            // Bước 3: Đưa danh sách option vào ViewBag để Create/Edit sử dụng.
            ViewBag.DrinkSelectList = drinkItems.Select(d => new SelectListItem { Value = d.Value, Text = d.Text }).ToList();
        }
    }
}
