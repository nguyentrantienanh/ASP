using Microsoft.AspNetCore.Mvc;
using TienAnhGold.Models;
using TienAnhGold.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using TienAnhGold.Extensions; // Namespace cho extension method

namespace TienAnhGold.Controllers
{
    [Authorize(Roles = "Employee")] // Chỉ nhân viên mới truy cập được
    public class EmployeeController : Controller
    {
        private readonly TienAnhGoldContext _context;
        private readonly ILogger<EmployeeController> _logger; // Thêm logger

        public EmployeeController(TienAnhGoldContext context, ILogger<EmployeeController> logger)
        {
            _context = context;
            _logger = logger; // Khởi tạo logger
        }

        // GET: Employee/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Employee/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email && e.IsActive);
                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for email: {Email}", email);
                    ModelState.AddModelError("", "Email không tồn tại hoặc tài khoản không hoạt động.");
                    return View();
                }

                if (!BCrypt.Net.BCrypt.Verify(password, employee.Password))
                {
                    _logger.LogWarning("Invalid password for email: {Email}", email);
                    ModelState.AddModelError("", "Mật khẩu không đúng.");
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, employee.Name ?? ""),
                    new Claim(ClaimTypes.Email, employee.Email ?? ""),
                    new Claim(ClaimTypes.Role, "Employee") // Đảm bảo vai trò Employee được gán
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                HttpContext.Session.SetInt32("EmployeeId", employee.Id);

                _logger.LogInformation("Employee {Email} logged in successfully.", email);
                return RedirectToAction("Dashboard", "Employee");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during employee login for email: {Email}", email);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.");
                return View();
            }
        }

        // GET: Employee/Logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Remove("EmployeeId"); // Xóa EmployeeId khỏi session
                _logger.LogInformation("Employee logged out successfully.");
                return RedirectToAction("Login", "Employee");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during employee logout.");
                return RedirectToAction("Login", "Employee");
            }
        }

        // GET: Employee/Dashboard (Chỉ nhân viên mới truy cập được)
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var dashboardViewModel = new DashboardViewModel
                {
                    OrderCount = await _context.Orders.CountAsync(), // Tổng số đơn hàng, không giới hạn trạng thái
                    GoldCount = await _context.Gold.CountAsync(), // Đếm số lượng sản phẩm vàng
                    RecentOrders = await _context.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .Take(5) // Lấy 5 đơn hàng gần đây
                        .ToListAsync()
                };

                _logger.LogInformation("Employee dashboard loaded successfully for user: {Email}", User.GetEmail());
                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee dashboard for user: {Email}", User.GetEmail());
                return View(new DashboardViewModel()); // Trả về model rỗng hoặc lỗi
            }
        }

        // GET: Employee/ManageOrders (Chỉ nhân viên mới truy cập được)
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> ManageOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .Where(o => o.Status == OrderStatus.Pending) // Chỉ hiển thị đơn hàng chưa xác nhận
                    .ToListAsync();

                _logger.LogInformation("Employee manage orders loaded successfully for user: {Email}", User.GetEmail());
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee manage orders.");
                return View(new List<Order>()); // Trả về danh sách rỗng hoặc lỗi
            }
        }

        // POST: Employee/ConfirmOrder
        // POST: Admin/ConfirmOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Order ID {Id} not found for confirmation by admin: {AdminEmail}", id, adminEmail);
                    return Json(new { success = false, message = "Đơn hàng không tồn tại." });
                }

                if (order.Status != OrderStatus.Pending)
                {
                    _logger.LogWarning("Order ID {Id} is not in Pending status for confirmation by admin: {AdminEmail}. Current status: {Status}", id, adminEmail, order.Status);
                    return Json(new { success = false, message = "Đơn hàng không ở trạng thái chờ xử lý." });
                }

                // Kiểm tra và cập nhật số lượng tồn kho
                foreach (var item in order.OrderDetails)
                {
                    if (item.Gold == null)
                    {
                        _logger.LogWarning("Gold item with ID {GoldId} not found for OrderDetail in Order ID {OrderId} by admin: {AdminEmail}", item.GoldId, id, adminEmail);
                        return Json(new { success = false, message = $"Sản phẩm với ID {item.GoldId} không tồn tại trong kho." });
                    }

                    if (item.Gold.Quantity < item.Quantity)
                    {
                        _logger.LogWarning("Insufficient quantity for Gold item ID {GoldId} in Order ID {OrderId}. Available: {Available}, Requested: {Requested} by admin: {AdminEmail}",
                            item.GoldId, id, item.Gold.Quantity, item.Quantity, adminEmail);
                        return Json(new { success = false, message = $"Số lượng trong kho không đủ cho sản phẩm {item.Gold.Name}." });
                    }

                    item.Gold.Quantity -= item.Quantity; // Giảm số lượng trong kho
                }

                // Cập nhật trạng thái và tổng tiền
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);
                order.Status = OrderStatus.Approved;
                order.ConfirmedAt = DateTime.Now; // Ghi lại thời gian xác nhận (nếu bạn có thuộc tính này trong model)

                await _context.SaveChangesAsync();
                _logger.LogInformation("Order ID {Id} confirmed to Approved successfully by admin: {AdminEmail}", id, adminEmail);

                return Json(new { success = true, message = "Đơn hàng đã được xác nhận. Chờ giao hàng!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order ID {Id} by admin: {AdminEmail}", id, adminEmail);
                return Json(new { success = false, message = $"Đã xảy ra lỗi khi xác nhận đơn hàng: {ex.Message}" });
            }
        }


        // GET: Employee/OrderDetails (Xem chi tiết đơn hàng, nếu cần)
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for employee: {Email}", id, User.GetEmail());
                    return NotFound();
                }

                _logger.LogInformation("Order details {OrderId} loaded successfully for employee: {Email}", id, User.GetEmail());
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details {OrderId} for employee: {Email}", id, User.GetEmail());
                return NotFound();
            }
        }

        // GET: Employee/Notifications
        [Authorize(Roles = "Employee")]
        public IActionResult Notifications()
        {
            // Logic giả lập: lấy danh sách thông báo (có thể từ database)
            var notifications = new List<string> { "Yêu cầu hỗ trợ từ người dùng: nttanh0412@gmail.com", "Tin nhắn từ admin: Vui lòng kiểm tra đơn hàng #123", "Thông báo mới: Cập nhật hệ thống" };
            ViewBag.Notifications = notifications;
            return View();
        }
    }
}