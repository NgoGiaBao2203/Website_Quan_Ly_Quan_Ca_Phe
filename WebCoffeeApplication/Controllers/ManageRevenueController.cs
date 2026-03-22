using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebCoffeeApplication.DataContext;
using WebCoffeeApplication.Models;

namespace WebCoffeeApplication.Controllers
{
  [Authorize(Roles = "ChuQuan")]
  public class ManageRevenueController(AppDataContext context) : Controller
  {
    private readonly AppDataContext _context = context;

    /// <summary>
    /// Hiển thị danh sách doanh thu từ các hóa đơn đã thanh toán và hỗ trợ lọc theo ngày/tháng/năm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? filterType, DateTime? selectedDate, int? selectedMonth, int? selectedYear)
    {
      // Bước 1: Chuẩn hóa kiểu lọc mặc định nếu chưa chọn.
      filterType = string.IsNullOrWhiteSpace(filterType) ? "day" : filterType.ToLower();

      // Bước 1.1: Chỉ validate khi người dùng thực sự bấm lọc.
      var isFilterSubmitted = Request.Query.ContainsKey("filterType")
          || Request.Query.ContainsKey("selectedDate")
          || Request.Query.ContainsKey("selectedMonth")
          || Request.Query.ContainsKey("selectedYear");

      // Bước 1.2: Kiểm tra đầu vào bắt buộc theo từng kiểu lọc.
      if (isFilterSubmitted)
      {
        var missingFields = new List<string>();

        if (filterType == "day")
        {
          if (!selectedDate.HasValue)
          {
            missingFields.Add("ngày");
          }
        }
        else if (filterType == "month")
        {
          if (!selectedMonth.HasValue)
          {
            missingFields.Add("tháng");
          }

          if (!selectedYear.HasValue)
          {
            missingFields.Add("năm");
          }
        }
        else if (filterType == "year")
        {
          if (!selectedYear.HasValue)
          {
            missingFields.Add("năm");
          }
        }

        if (missingFields.Count > 0)
        {
          TempData["ToastMessage"] = $"Vui lòng nhập đầy đủ thông tin lọc: {string.Join(", ", missingFields)}.";
          TempData["ToastType"] = "warning";
        }
      }

      // Bước 2: Khởi tạo truy vấn doanh thu chỉ lấy hóa đơn đã thanh toán.
      var paidOrdersQuery = _context.HoaDon
          .Include(h => h.TaiKhoan)
          .Include(h => h.ChiTietHoaDons)
          .Where(h => h.TrangThaiXoa != 1 && h.TrangThai == TrangThaiHoaDon.DaThanhToan)
          .AsQueryable();

      // Bước 3: Áp dụng bộ lọc theo kiểu người dùng chọn.
      if (filterType == "day" && selectedDate.HasValue)
      {
        var day = selectedDate.Value.Date;
        paidOrdersQuery = paidOrdersQuery.Where(h => h.NgayTao.HasValue && h.NgayTao.Value.Date == day);
      }
      else if (filterType == "month" && selectedMonth.HasValue && selectedYear.HasValue)
      {
        var month = selectedMonth.Value;
        var year = selectedYear.Value;
        paidOrdersQuery = paidOrdersQuery.Where(h =>
            h.NgayTao.HasValue
            && h.NgayTao.Value.Month == month
            && h.NgayTao.Value.Year == year);
      }
      else if (filterType == "year" && selectedYear.HasValue)
      {
        var year = selectedYear.Value;
        paidOrdersQuery = paidOrdersQuery.Where(h => h.NgayTao.HasValue && h.NgayTao.Value.Year == year);
      }

      // Bước 4: Sắp xếp từ mới nhất đến cũ nhất và thực thi truy vấn.
      var paidOrders = await paidOrdersQuery
          .OrderByDescending(h => h.NgayTao)
          .ToListAsync();

      // Bước 5: Tính tổng doanh thu của danh sách hiện tại để hiển thị nhanh trên UI.
      var totalRevenue = paidOrders.Sum(h => h.TongTien ?? 0);

      // Bước 6: Lấy danh sách năm có hóa đơn đã thanh toán để hiển thị dropdown lọc theo năm.
      var availableYears = await _context.HoaDon
          .Where(h => h.TrangThaiXoa != 1
              && h.TrangThai == TrangThaiHoaDon.DaThanhToan
              && h.NgayTao.HasValue)
          .Select(h => h.NgayTao!.Value.Year)
          .Distinct()
          .OrderByDescending(y => y)
          .ToListAsync();

      // Đảm bảo năm hiện tại luôn có trong danh sách.
      var currentYear = DateTime.Now.Year;
      if (!availableYears.Contains(currentYear))
      {
        availableYears.Insert(0, currentYear);
      }

      // Bước 7: Truyền thông tin lọc và tổng doanh thu sang View.
      ViewData["FilterType"] = filterType;
      ViewData["SelectedDate"] = selectedDate?.ToString("yyyy-MM-dd");
      ViewData["SelectedMonth"] = selectedMonth;
      ViewData["SelectedYear"] = selectedYear;
      ViewData["TotalRevenue"] = totalRevenue;
      ViewData["AvailableYears"] = availableYears;

      // Bước 8: Trả về danh sách doanh thu đã lọc.
      return View(paidOrders);
    }

    /// <summary>
    /// Hiển thị chi tiết doanh thu theo từng hóa đơn đã thanh toán
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
      // Bước 1: Kiểm tra id hợp lệ.
      if (id == null)
      {
        return NotFound();
      }

      // Bước 2: Tải đầy đủ hóa đơn đã thanh toán cùng danh sách sản phẩm.
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

      // Bước 4: Chỉ cho phép xem chi tiết doanh thu của hóa đơn đã thanh toán.
      if (order.TrangThai != TrangThaiHoaDon.DaThanhToan)
      {
        TempData["ToastMessage"] = "Chỉ có thể xem doanh thu từ hóa đơn đã thanh toán.";
        TempData["ToastType"] = "warning";
        return RedirectToAction(nameof(Index));
      }

      // Bước 5: Trả về màn hình chi tiết doanh thu.
      return View(order);
    }
  }
}
