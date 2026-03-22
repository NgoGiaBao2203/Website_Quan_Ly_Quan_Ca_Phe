using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebCoffeeApplication.DataContext;
using WebCoffeeApplication.Helpers;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
  [Authorize]
  public class ManageCategoryController(AppDataContext context) : Controller
  {
    private readonly AppDataContext _context = context;

    /// <summary>
    /// Hiển thị danh sách tất cả các danh mục đồ uống với phân trang
    /// </summary>
    public async Task<IActionResult> Index(int? pageNumber)
    {
      int pageSize = 4;

      var isChuQuan = User.IsInRole("ChuQuan");
      ViewBag.IsChuQuan = isChuQuan;

      // Bước 1: Truy vấn danh sách tất cả các danh mục đồ uống kèm theo đồ uống liên quan từ cơ sở dữ liệu
      var categories = _context.DanhMucDoUong
          .Include(c => c.DoUongs.Where(d => d.TrangThaiXoa != 1))
          .AsQueryable();

      // NhanVien không được xem danh mục đã xóa mềm.
      if (!isChuQuan)
      {
        categories = categories.Where(c => c.TrangThaiXoa != 1);
      }

      // Bước 2: Thực thi truy vấn với phân trang
      var result = await PaginatedList<DanhMucDoUong>.CreateAsync(categories, pageNumber ?? 1, pageSize);

      // Bước 3: Trả về View cùng với dữ liệu danh sách danh mục để hiển thị lên màn hình
      return View(result);
    }

    /// <summary>
    /// Hiển thị thông tin chi tiết của một danh mục đồ uống
    /// </summary>
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var category = await _context.DanhMucDoUong
          .Include(c => c.DoUongs.Where(d => d.TrangThaiXoa != 1))
          .FirstOrDefaultAsync(m => m.MaDanhMucDoUong == id);

      if (category == null)
      {
        return NotFound();
      }

      // NhanVien không được xem danh mục đã xóa mềm.
      if (!User.IsInRole("ChuQuan") && category.TrangThaiXoa == 1)
      {
        return NotFound();
      }

      ViewBag.IsChuQuan = User.IsInRole("ChuQuan");

      return View(category);
    }

    /// <summary>
    /// Hiển thị giao diện form để thêm mới một danh mục đồ uống
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
      // Trả về View chứa form nhập liệu cho việc điều thông tin danh mục mới
      return View();
    }

    /// <summary>
    /// Xử lý dữ liệu được gửi từ form thêm mới danh mục
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken] // Bảo vệ ứng dụng khỏi các cuộc tấn công CSRF
    public async Task<IActionResult> Create(DanhMucDoUong model)
    {
      // Bước 1: Kiểm tra danh mục đã tồn tại theo TenDanhMuc
      if (!string.IsNullOrWhiteSpace(model.TenDanhMuc))
      {
        var categoryExists = await _context.DanhMucDoUong
            .AnyAsync(c => c.TrangThaiXoa != 1 && c.TenDanhMuc != null && c.TenDanhMuc == model.TenDanhMuc.Trim());

        if (categoryExists)
        {
          TempData["ToastMessage"] = $"Danh mục \"{model.TenDanhMuc.Trim()}\" đã tồn tại.";
          TempData["ToastType"] = "warning";
          return View(model);
        }
      }

      // Bước 2: Kiểm tra xem dữ liệu người dùng nhập có hợp lệ với các ràng buộc trong Model không
      if (ModelState.IsValid)
      {
        // Bước 2: Thêm đối tượng danh mục mới vào Entity Framework tracking
        _context.DanhMucDoUong.Add(model);
        // Bước 3: Lưu lại thay đổi xuống cơ sở dữ liệu
        await _context.SaveChangesAsync();
        // Bước 4: Chuyển hướng người dùng về trang danh sách (Action Index)
        TempData["ToastMessage"] = "Thêm danh mục thành công!";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
      }
      // Bước 5: Nếu dữ liệu không hợp lệ, trả lại View kèm theo dữ liệu mà họ đã nhập cùng các thông báo lỗi
      return View(model);
    }

    /// <summary>
    /// Hiển thị giao diện form để cập nhật thông tin một danh mục đồ uống đã có
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
      // Bước 1: Kiểm tra xem id truyền vào có null không. Nếu có thì trả về trang lỗi 404 (Không tìm thấy)
      if (id == null)
      {
        return NotFound();
      }

      // Bước 2: Tìm kiếm danh mục đồ uống trong cơ sở dữ liệu dựa trên id được cung cấp
      var category = await _context.DanhMucDoUong.FindAsync(id);

      // Bước 3: Nếu không tìm thấy danh mục đồ uống, tiếp tục trả về trang lỗi 404
      if (category == null)
      {
        return NotFound();
      }

      // NhanVien không được sửa danh mục đã xóa mềm.
      var isChuQuan = User.IsInRole("ChuQuan");
      if (!isChuQuan && category.TrangThaiXoa == 1)
      {
        return NotFound();
      }

      ViewBag.IsChuQuan = isChuQuan;

      // Bước 4: Trả về View kèm theo dữ liệu của danh mục đồ uống vừa tìm thấy để điền vào form
      return View(category);
    }

    /// <summary>
    /// Xử lý dữ liệu được gửi từ form cập nhật danh mục đồ uống để cập nhật vào cơ sở dữ liệu
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DanhMucDoUong model)
    {
      // Bước 1: Đảm bảo phần id đang cập nhật khớp với mã danh mục đồ uống nằm bên trong dữ liệu nhận được
      if (id != model.MaDanhMucDoUong)
      {
        return NotFound();
      }

      // Bước 2: Kiểm tra dữ liệu đưa vào có hợp lệ hay không
      if (ModelState.IsValid)
      {
        try
        {
          // Bước 3: Lấy dữ liệu cũ kèm đồ uống liên quan để xử lý thay đổi TrangThaiXoa.
          var existing = await _context.DanhMucDoUong
              .Include(c => c.DoUongs)
              .FirstOrDefaultAsync(c => c.MaDanhMucDoUong == id);
          if (existing == null)
          {
            return NotFound();
          }

          // NhanVien không được thay đổi TrangThaiXoa.
          if (!User.IsInRole("ChuQuan"))
          {
            model.TrangThaiXoa = existing.TrangThaiXoa;
          }

          // Bước 3.1: Xử lý thay đổi TrangThaiXoa — cascade sang tất cả đồ uống thuộc danh mục.
          if (model.TrangThaiXoa != existing.TrangThaiXoa)
          {
            if (model.TrangThaiXoa == 1)
            {
              // Xóa mềm: kiểm tra hóa đơn chưa thanh toán trước khi cho phép.
              var drinkIds = existing.DoUongs.Where(d => d.TrangThaiXoa != 1).Select(d => d.MaDoUong).ToList();
              if (drinkIds.Count > 0)
              {
                var drinksInUnpaidInvoices = await _context.ChiTietHoaDon
                    .Where(ct => ct.MaDoUong != null
                        && drinkIds.Contains(ct.MaDoUong.Value)
                        && ct.HoaDon != null
                        && ct.HoaDon.TrangThaiXoa != 1
                        && (ct.HoaDon.TrangThai == TrangThaiHoaDon.DangXuLy
                            || ct.HoaDon.TrangThai == TrangThaiHoaDon.DaLenMon))
                    .Select(ct => ct.DoUong!.TenDoUong)
                    .Distinct()
                    .ToListAsync();

                if (drinksInUnpaidInvoices.Count > 0)
                {
                  var drinkNames = string.Join(", ", drinksInUnpaidInvoices);
                  ModelState.AddModelError("TrangThaiXoa",
                      $"Không thể xóa danh mục vì đồ uống ({drinkNames}) đang có trong hóa đơn chưa thanh toán.");
                  return View(model);
                }
              }

              // Đánh dấu xóa mềm cho tất cả đồ uống thuộc danh mục.
              foreach (var drink in existing.DoUongs.Where(d => d.TrangThaiXoa != 1))
              {
                drink.TrangThaiXoa = 1;
              }
            }
            else
            {
              // Khôi phục: bỏ đánh dấu xóa mềm cho tất cả đồ uống thuộc danh mục.
              foreach (var drink in existing.DoUongs.Where(d => d.TrangThaiXoa == 1))
              {
                drink.TrangThaiXoa = 0;
              }
            }
          }

          // Bước 4: Cập nhật thông tin danh mục.
          existing.TenDanhMuc = model.TenDanhMuc;
          existing.TrangThaiXoa = model.TrangThaiXoa;

          // Bước 5: Lưu thay đổi thông tin danh mục xuống cơ sở dữ liệu
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          // Bước 5: Trong trường hợp xảy ra xung đột khi cập nhật, kiểm tra xem danh mục đó có còn tồn tại trong cơ sở dữ liệu không
          if (!await _context.DanhMucDoUong.AnyAsync(e => e.MaDanhMucDoUong == id))
          {
            return NotFound();
          }
          throw;
        }
        // Bước 6: Nếu quá trình cập nhật diễn ra thành công, chuyển hướng người dùng về trang danh sách (Action Index)
        TempData["ToastMessage"] = "Cập nhật danh mục thành công!";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
      }
      // Bước 7: Trả lại View và dữ liệu ban đầu cho người dùng nếu có lỗi xảy ra
      return View(model);
    }

    /// <summary>
    /// Hiển thị trang để xác nhận xóa đi một danh mục đồ uống
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
      // Bước 1: Kiểm tra xem tham số id có cung cấp hay không. Nếu không, trả về trang lỗi 404
      if (id == null)
      {
        return NotFound();
      }

      // Bước 2: Truy vấn cơ sở dữ liệu để tìm danh mục đồ uống tương ứng với id, bao gồm các đồ uống liên quan
      var category = await _context.DanhMucDoUong
          .Include(c => c.DoUongs.Where(d => d.TrangThaiXoa != 1))
          .FirstOrDefaultAsync(m => m.MaDanhMucDoUong == id && m.TrangThaiXoa != 1);

      // Bước 3: Kiểm tra nếu việc tìm kiếm thất bại, tức là không có danh mục đồ uống này
      if (category == null)
      {
        return NotFound();
      }

      // Bước 4: Truyền số lượng đồ uống liên quan để hiển thị cảnh báo
      ViewBag.SoLuongDoUong = category.DoUongs.Count;

      // Bước 5: Trả danh mục đồ uống về View để người dùng xác nhận quyết định xóa
      return View(category);
    }

    /// <summary>
    /// Tiến hành xóa chính thức danh mục đồ uống và tất cả đồ uống liên quan từ cơ sở dữ liệu
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      // Bước 1: Tìm danh mục đồ uống kèm theo tất cả đồ uống liên quan
      var category = await _context.DanhMucDoUong
          .Include(c => c.DoUongs)
          .FirstOrDefaultAsync(c => c.MaDanhMucDoUong == id && c.TrangThaiXoa != 1);

      if (category != null)
      {
        // Bước 2: Kiểm tra xem có đồ uống nào trong danh mục đang nằm trong hóa đơn chưa thanh toán không
        var drinkIds = category.DoUongs.Where(d => d.TrangThaiXoa != 1).Select(d => d.MaDoUong).ToList();
        var drinksInUnpaidInvoices = await _context.ChiTietHoaDon
            .Where(ct => ct.MaDoUong != null
                && drinkIds.Contains(ct.MaDoUong.Value)
                && ct.HoaDon != null
                && ct.HoaDon.TrangThaiXoa != 1
                && (ct.HoaDon.TrangThai == TrangThaiHoaDon.DangXuLy
                    || ct.HoaDon.TrangThai == TrangThaiHoaDon.DaLenMon))
            .Select(ct => ct.DoUong!.TenDoUong)
            .Distinct()
            .ToListAsync();

        if (drinksInUnpaidInvoices.Count > 0)
        {
          var drinkNames = string.Join(", ", drinksInUnpaidInvoices);
          TempData["ToastMessage"] = $"Không thể xóa danh mục \"{category.TenDanhMuc}\" vì đồ uống ({drinkNames}) đang có trong hóa đơn chưa thanh toán.";
          TempData["ToastType"] = "warning";
          return RedirectToAction(nameof(Index));
        }

        // Bước 3: Đánh dấu xóa mềm cho tất cả đồ uống thuộc danh mục
        foreach (var drink in category.DoUongs.Where(d => d.TrangThaiXoa != 1))
        {
          drink.TrangThaiXoa = 1;
        }

        // Bước 4: Đánh dấu xóa mềm cho danh mục đồ uống
        category.TrangThaiXoa = 1;

        // Bước 5: Lưu tất cả thay đổi vào cơ sở dữ liệu
        await _context.SaveChangesAsync();

        TempData["ToastMessage"] = "Xóa danh mục thành công!";
        TempData["ToastType"] = "success";
      }
      else
      {
        TempData["ToastMessage"] = "Không tìm thấy danh mục cần xóa.";
        TempData["ToastType"] = "danger";
      }
      // Bước 6: Chuyển hướng người dùng về trang danh sách (Action Index) sau khi xóa hoàn tất
      return RedirectToAction(nameof(Index));
    }
  }
}
