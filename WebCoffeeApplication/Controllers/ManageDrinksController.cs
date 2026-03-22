using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebCoffeeApplication.DataContext;
using WebCoffeeApplication.Helpers;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
  [Authorize]
  public class ManageDrinksController(AppDataContext context, IWebHostEnvironment webHostEnvironment) : Controller
  {
    private readonly AppDataContext _context = context;
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

    /// <summary>
    /// Hiển thị danh sách đồ uống, hỗ trợ tìm kiếm theo tên và phân trang
    /// </summary>
    public async Task<IActionResult> Index(string? searchString, int? pageNumber)
    {
      int pageSize = 4;

      var isChuQuan = User.IsInRole("ChuQuan");
      ViewBag.IsChuQuan = isChuQuan;

      // Bước 1: Tạo truy vấn cơ sở, bao gồm thông tin danh mục đồ uống
      var drinks = _context.DoUong
          .Include(d => d.ChiTietDoUongs)
          .AsQueryable();

      // NhanVien không được xem đồ uống đã xóa mềm.
      if (!isChuQuan)
      {
        drinks = drinks.Where(d => d.TrangThaiXoa != 1);
      }

      // Bước 2: Nếu có từ khóa tìm kiếm, lọc theo tên đồ uống
      if (!string.IsNullOrEmpty(searchString))
      {
        drinks = drinks.Where(d => d.TenDoUong != null && d.TenDoUong.Contains(searchString));
      }

      // Bước 3: Lưu lại từ khóa tìm kiếm để hiển thị trên giao diện
      ViewData["SearchString"] = searchString;

      // Bước 4: Đếm tổng số kết quả để kiểm tra tìm kiếm
      var totalCount = await drinks.CountAsync();

      // Bước 5: Nếu tìm kiếm không có kết quả, gửi thông báo toast và hiển thị toàn bộ danh sách
      if (!string.IsNullOrEmpty(searchString) && totalCount == 0)
      {
        TempData["ToastMessage"] = "Không tìm thấy đồ uống nào phù hợp với từ khóa tìm kiếm.";
        TempData["ToastType"] = "warning";

        // Trả về danh sách đầy đủ khi không tìm thấy kết quả
        drinks = _context.DoUong
            .Include(d => d.DanhMucDoUong)
            .Include(d => d.ChiTietDoUongs)
            .AsQueryable();
        if (!isChuQuan)
        {
          drinks = drinks.Where(d => d.TrangThaiXoa != 1);
        }
        ViewData["SearchString"] = null;
      }
      else if (!string.IsNullOrEmpty(searchString))
      {
        TempData["ToastMessage"] = "Đã tìm thấy đồ uống phù hợp với từ khóa tìm kiếm.";
        TempData["ToastType"] = "success";
      }

      // Bước 6: Thực thi truy vấn với phân trang
      var result = await PaginatedList<DoUong>.CreateAsync(drinks, pageNumber ?? 1, pageSize);

      // Bước 7: Trả về View cùng với danh sách đồ uống
      return View(result);
    }

    /// <summary>
    /// Hiển thị chi tiết một đồ uống bao gồm các kích cỡ và giá bán
    /// </summary>
    public async Task<IActionResult> Details(int? id)
    {
      // Bước 1: Kiểm tra xem id truyền vào có null không
      if (id == null)
      {
        return NotFound();
      }

      // Bước 2: Tìm đồ uống theo id, bao gồm danh mục và chi tiết kích cỡ
      var drink = await _context.DoUong
          .Include(d => d.DanhMucDoUong)
          .Include(d => d.ChiTietDoUongs)
          .FirstOrDefaultAsync(d => d.MaDoUong == id);

      // Bước 3: Nếu không tìm thấy, trả về trang lỗi 404
      if (drink == null)
      {
        return NotFound();
      }

      // NhanVien không được xem đồ uống đã xóa mềm.
      if (!User.IsInRole("ChuQuan") && drink.TrangThaiXoa == 1)
      {
        return NotFound();
      }

      ViewBag.IsChuQuan = User.IsInRole("ChuQuan");

      // Bước 4: Trả về View kèm theo dữ liệu đồ uống
      return View(drink);
    }

    /// <summary>
    /// Hiển thị giao diện form để thêm mới một đồ uống
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
      // Chuẩn bị danh sách danh mục đồ uống cho dropdown
      ViewBag.DanhMucList = new SelectList(await _context.DanhMucDoUong.Where(c => c.TrangThaiXoa != 1).ToListAsync(), "MaDanhMucDoUong", "TenDanhMuc");
      return View();
    }

    /// <summary>
    /// Xử lý dữ liệu được gửi từ form thêm mới đồ uống
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoUong model, decimal? giaBan, string? kichCo, IFormFile? imageFile)
    {
      // Bước 1: Loại bỏ các thuộc tính điều hướng khỏi kiểm tra ModelState
      ModelState.Remove("DanhMucDoUong");
      ModelState.Remove("ChiTietHoaDons");
      ModelState.Remove("ChiTietDoUongs");
      ModelState.Remove("LinkHinhAnh");

      if (!giaBan.HasValue)
      {
        ModelState.AddModelError("giaBan", "Vui lòng nhập giá bán.");
      }
      if (string.IsNullOrWhiteSpace(kichCo))
      {
        ModelState.AddModelError("kichCo", "Vui lòng nhập kích cỡ.");
      }
      if (imageFile == null || imageFile.Length == 0)
      {
        ModelState.AddModelError("imageFile", "Vui lòng chọn hình ảnh.");
      }

      // Bước 2: Kiểm tra đồ uống đã tồn tại theo TenDoUong và KichCo
      if (!string.IsNullOrWhiteSpace(model.TenDoUong) && !string.IsNullOrWhiteSpace(kichCo))
      {
        var normalizedKichCo = kichCo.Trim().ToUpper();
        var drinkExists = await _context.DoUong
            .AnyAsync(d => d.TrangThaiXoa != 1
                && d.TenDoUong != null
                && d.TenDoUong == model.TenDoUong.Trim()
                && d.ChiTietDoUongs.Any(ct => ct.KichCo == normalizedKichCo));

        if (drinkExists)
        {
          TempData["ToastMessage"] = $"Đồ uống \"{model.TenDoUong.Trim()}\" với kích cỡ \"{normalizedKichCo}\" đã tồn tại.";
          TempData["ToastType"] = "warning";
          ViewBag.GiaBan = FormatGiaBan(giaBan);
          ViewBag.KichCo = kichCo;
          ViewBag.DanhMucList = new SelectList(await _context.DanhMucDoUong.Where(c => c.TrangThaiXoa != 1).ToListAsync(), "MaDanhMucDoUong", "TenDanhMuc", model.MaDanhMucDoUong);
          return View(model);
        }
      }

      // Bước 3: Kiểm tra dữ liệu hợp lệ
      if (ModelState.IsValid)
      {
        // Bước 4: Lưu hình ảnh vào thư mục wwwroot/images/drinks
        if (imageFile != null && imageFile.Length > 0)
        {
          model.LinkHinhAnh = await SaveImageAsync(imageFile);
        }

        // Bước 5: Thêm đồ uống mới vào cơ sở dữ liệu
        _context.DoUong.Add(model);
        await _context.SaveChangesAsync();

        if (giaBan.HasValue && !string.IsNullOrWhiteSpace(kichCo))
        {
          _context.ChiTietDoUong.Add(new ChiTietDoUong
          {
            MaDoUong = model.MaDoUong,
            KichCo = kichCo.Trim().ToUpper(),
            GiaBan = giaBan
          });
          await _context.SaveChangesAsync();
        }

        // Bước 6: Chuyển hướng về trang danh sách
        TempData["ToastMessage"] = "Thêm đồ uống thành công!";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
      }

      // Bước 6: Nếu dữ liệu không hợp lệ, trả lại View kèm danh sách danh mục
      ViewBag.GiaBan = FormatGiaBan(giaBan);
      ViewBag.KichCo = kichCo;
      ViewBag.DanhMucList = new SelectList(await _context.DanhMucDoUong.Where(c => c.TrangThaiXoa != 1).ToListAsync(), "MaDanhMucDoUong", "TenDanhMuc", model.MaDanhMucDoUong);
      return View(model);
    }

    /// <summary>
    /// Hiển thị giao diện form để cập nhật thông tin một đồ uống
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
      // Bước 1: Kiểm tra id
      if (id == null)
      {
        return NotFound();
      }

      var isChuQuan = User.IsInRole("ChuQuan");

      // Bước 2: Tìm đồ uống theo id
      var drink = await _context.DoUong
          .Include(d => d.ChiTietDoUongs)
          .FirstOrDefaultAsync(d => d.MaDoUong == id);
      if (drink == null)
      {
        return NotFound();
      }

      // NhanVien không được sửa đồ uống đã xóa mềm.
      if (!isChuQuan && drink.TrangThaiXoa == 1)
      {
        return NotFound();
      }

      ViewBag.IsChuQuan = isChuQuan;
      ViewBag.GiaBan = FormatGiaBan(drink.ChiTietDoUongs.FirstOrDefault()?.GiaBan);
      ViewBag.KichCo = drink.ChiTietDoUongs.FirstOrDefault()?.KichCo;

      // Bước 3: Chuẩn bị danh sách danh mục cho dropdown
      ViewBag.DanhMucList = new SelectList(await _context.DanhMucDoUong.Where(c => c.TrangThaiXoa != 1).ToListAsync(), "MaDanhMucDoUong", "TenDanhMuc", drink.MaDanhMucDoUong);
      return View(drink);
    }

    /// <summary>
    /// Xử lý dữ liệu được gửi từ form cập nhật đồ uống
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DoUong model, decimal? giaBan, string? kichCo, IFormFile? imageFile)
    {
      // Bước 1: Đảm bảo id khớp với mã đồ uống
      if (id != model.MaDoUong)
      {
        return NotFound();
      }

      // Bước 2: Loại bỏ các thuộc tính điều hướng khỏi kiểm tra ModelState
      ModelState.Remove("DanhMucDoUong");
      ModelState.Remove("ChiTietHoaDons");
      ModelState.Remove("ChiTietDoUongs");
      ModelState.Remove("LinkHinhAnh");

      if (!giaBan.HasValue)
      {
        ModelState.AddModelError("giaBan", "Vui lòng nhập giá bán.");
      }
      if (string.IsNullOrWhiteSpace(kichCo))
      {
        ModelState.AddModelError("kichCo", "Vui lòng nhập kích cỡ.");
      }

      // Bước 2.1: Kiểm tra đồ uống đã tồn tại theo TenDoUong và KichCo (loại trừ chính đồ uống đang sửa)
      if (!string.IsNullOrWhiteSpace(model.TenDoUong) && !string.IsNullOrWhiteSpace(kichCo))
      {
        var normalizedKichCo = kichCo.Trim().ToUpper();
        var drinkExists = await _context.DoUong
            .AnyAsync(d => d.MaDoUong != id
                && d.TrangThaiXoa != 1
                && d.TenDoUong != null
                && d.TenDoUong == model.TenDoUong.Trim()
                && d.ChiTietDoUongs.Any(ct => ct.KichCo == normalizedKichCo));

        if (drinkExists)
        {
          TempData["ToastMessage"] = $"Đồ uống \"{model.TenDoUong.Trim()}\" với kích cỡ \"{normalizedKichCo}\" đã tồn tại.";
          TempData["ToastType"] = "warning";
          // Giữ lại hình ảnh hiện tại từ cơ sở dữ liệu vì form không gửi lại LinkHinhAnh.
          var currentDrink = await _context.DoUong.AsNoTracking()
              .FirstOrDefaultAsync(d => d.MaDoUong == id);
          model.LinkHinhAnh = currentDrink?.LinkHinhAnh;
          ViewBag.GiaBan = FormatGiaBan(giaBan);
          ViewBag.KichCo = kichCo;
          ViewBag.IsChuQuan = User.IsInRole("ChuQuan");
          ViewBag.DanhMucList = new SelectList(await _context.DanhMucDoUong.Where(c => c.TrangThaiXoa != 1).ToListAsync(), "MaDanhMucDoUong", "TenDanhMuc", model.MaDanhMucDoUong);
          return View(model);
        }
      }

      // Bước 3: Kiểm tra dữ liệu hợp lệ
      if (ModelState.IsValid)
      {
        try
        {
          // Bước 4: Lấy dữ liệu cũ để giữ lại thông tin không cho sửa.
          var existingDrink = await _context.DoUong.AsNoTracking()
              .FirstOrDefaultAsync(d => d.MaDoUong == id);
          if (existingDrink == null)
          {
            return NotFound();
          }

          // NhanVien không được thay đổi TrangThaiXoa.
          if (!User.IsInRole("ChuQuan"))
          {
            model.TrangThaiXoa = existingDrink.TrangThaiXoa;
          }

          // Bước 4.1: Kiểm tra nếu thay đổi TrangThaiXoa sang xóa mềm, phải kiểm tra hóa đơn chưa thanh toán.
          if (model.TrangThaiXoa == 1 && existingDrink.TrangThaiXoa != 1)
          {
            var hasUnpaidInvoice = await _context.ChiTietHoaDon
                .AnyAsync(ct => ct.MaDoUong == id
                    && ct.HoaDon != null
                    && ct.HoaDon.TrangThaiXoa != 1
                    && (ct.HoaDon.TrangThai == TrangThaiHoaDon.DangXuLy
                        || ct.HoaDon.TrangThai == TrangThaiHoaDon.DaLenMon));

            if (hasUnpaidInvoice)
            {
              TempData["ToastMessage"] = "Không thể xóa đồ uống vì đang có hóa đơn chưa thanh toán chứa đồ uống này.";
              TempData["ToastType"] = "warning";
              model.TrangThaiXoa = existingDrink.TrangThaiXoa;
              model.LinkHinhAnh = existingDrink.LinkHinhAnh;
              ViewBag.IsChuQuan = User.IsInRole("ChuQuan");
              ViewBag.GiaBan = FormatGiaBan(giaBan);
              ViewBag.KichCo = kichCo;
              ViewBag.DanhMucList = new SelectList(await _context.DanhMucDoUong.Where(c => c.TrangThaiXoa != 1).ToListAsync(), "MaDanhMucDoUong", "TenDanhMuc", model.MaDanhMucDoUong);
              return View(model);
            }
          }

          // Bước 4.2: Xử lý hình ảnh mới nếu có upload
          if (imageFile != null && imageFile.Length > 0)
          {
            // Xóa hình ảnh cũ nếu tồn tại
            DeleteImage(existingDrink.LinkHinhAnh);
            model.LinkHinhAnh = await SaveImageAsync(imageFile);
          }
          else
          {
            // Giữ nguyên hình ảnh cũ
            model.LinkHinhAnh = existingDrink.LinkHinhAnh;
          }

          // Bước 5: Cập nhật đồ uống trong cơ sở dữ liệu
          _context.Update(model);
          await _context.SaveChangesAsync();

          var chiTietDoUong = await _context.ChiTietDoUong
              .FirstOrDefaultAsync(c => c.MaDoUong == model.MaDoUong);

          if (giaBan.HasValue && !string.IsNullOrWhiteSpace(kichCo))
          {
            if (chiTietDoUong == null)
            {
              _context.ChiTietDoUong.Add(new ChiTietDoUong
              {
                MaDoUong = model.MaDoUong,
                KichCo = kichCo.Trim().ToUpper(),
                GiaBan = giaBan
              });
            }
            else
            {
              chiTietDoUong.KichCo = kichCo.Trim().ToUpper();
              chiTietDoUong.GiaBan = giaBan;
              _context.ChiTietDoUong.Update(chiTietDoUong);
            }

            await _context.SaveChangesAsync();
          }
        }
        catch (DbUpdateConcurrencyException)
        {
          // Bước 6: Kiểm tra đồ uống còn tồn tại không
          if (!await _context.DoUong.AnyAsync(e => e.MaDoUong == id))
          {
            return NotFound();
          }
          throw;
        }
        // Bước 7: Chuyển hướng về trang danh sách
        TempData["ToastMessage"] = "Cập nhật đồ uống thành công!";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
      }

      // Bước 8: Trả lại View nếu có lỗi
      ViewBag.GiaBan = FormatGiaBan(giaBan);
      ViewBag.KichCo = kichCo;
      ViewBag.DanhMucList = new SelectList(await _context.DanhMucDoUong.Where(c => c.TrangThaiXoa != 1).ToListAsync(), "MaDanhMucDoUong", "TenDanhMuc", model.MaDanhMucDoUong);
      return View(model);
    }

    /// <summary>
    /// Hiển thị trang xác nhận xóa đồ uống
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
      // Bước 1: Kiểm tra id
      if (id == null)
      {
        return NotFound();
      }

      // Bước 2: Tìm đồ uống theo id, bao gồm danh mục
      var drink = await _context.DoUong
          .Include(d => d.DanhMucDoUong)
          .FirstOrDefaultAsync(d => d.MaDoUong == id && d.TrangThaiXoa != 1);

      // Bước 3: Nếu không tìm thấy, trả về 404
      if (drink == null)
      {
        return NotFound();
      }

      // Bước 4: Trả về View xác nhận xóa
      return View(drink);
    }

    /// <summary>
    /// Tiến hành xóa chính thức đồ uống khỏi cơ sở dữ liệu
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      // Bước 1: Tìm đồ uống theo id
      var drink = await _context.DoUong
          .FirstOrDefaultAsync(d => d.MaDoUong == id && d.TrangThaiXoa != 1);

      if (drink != null)
      {
        // Bước 2: Kiểm tra đồ uống có nằm trong hóa đơn chưa thanh toán không
        var hasUnpaidInvoice = await _context.ChiTietHoaDon
            .AnyAsync(ct => ct.MaDoUong == id
                && ct.HoaDon != null
                && ct.HoaDon.TrangThaiXoa != 1
                && (ct.HoaDon.TrangThai == TrangThaiHoaDon.DangXuLy
                    || ct.HoaDon.TrangThai == TrangThaiHoaDon.DaLenMon));

        if (hasUnpaidInvoice)
        {
          TempData["ToastMessage"] = $"Không thể xóa đồ uống \"{drink.TenDoUong}\" vì đang có hóa đơn chưa thanh toán chứa đồ uống này.";
          TempData["ToastType"] = "warning";
          return RedirectToAction(nameof(Index));
        }

        // Bước 3: Đánh dấu xóa mềm cho đồ uống
        drink.TrangThaiXoa = 1;

        // Bước 4: Lưu thay đổi
        await _context.SaveChangesAsync();

        TempData["ToastMessage"] = "Xóa đồ uống thành công!";
        TempData["ToastType"] = "success";
      }
      else
      {
        TempData["ToastMessage"] = "Không tìm thấy đồ uống cần xóa.";
        TempData["ToastType"] = "danger";
      }

      // Bước 5: Chuyển hướng về trang danh sách
      return RedirectToAction(nameof(Index));
    }

    private async Task<string> SaveImageAsync(IFormFile imageFile)
    {
      var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "drinks");
      Directory.CreateDirectory(uploadsFolder);

      var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
      var filePath = Path.Combine(uploadsFolder, uniqueFileName);

      using (var fileStream = new FileStream(filePath, FileMode.Create))
      {
        await imageFile.CopyToAsync(fileStream);
      }

      return "/images/drinks/" + uniqueFileName;
    }

    private void DeleteImage(string? imagePath)
    {
      if (string.IsNullOrEmpty(imagePath))
      {
        return;
      }

      var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
      if (System.IO.File.Exists(fullPath))
      {
        System.IO.File.Delete(fullPath);
      }
    }

    private static string? FormatGiaBan(decimal? giaBan)
    {
      if (!giaBan.HasValue)
      {
        return null;
      }

      return giaBan.Value.ToString("0.##", CultureInfo.InvariantCulture);
    }
  }
}
