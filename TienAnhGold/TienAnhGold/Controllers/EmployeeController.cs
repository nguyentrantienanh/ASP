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
                    OrderCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending), // Đơn chờ xác nhận
                    GoldCount = await _context.Gold.CountAsync() // Đếm số lượng sản phẩm vàng
                };

                _logger.LogInformation("Employee dashboard loaded successfully for user: {Email}", User.GetEmail());
                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee dashboard.");
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
        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order != null && order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Approved; // Thay IsConfirmed = true bằng Status = Approved


                    // Giảm số lượng tồn kho
                    foreach (var detail in order.OrderDetails)
                    {
                        var gold = await _context.Gold.FindAsync(detail.GoldId);
                        if (gold != null && gold.Quantity >= detail.Quantity)
                        {
                            gold.Quantity -= detail.Quantity;
                            if (gold.Quantity < 0) gold.Quantity = 0; // Đảm bảo không âm
                        }
                        else
                        {
                            _logger.LogWarning("Insufficient stock for Gold ID {GoldId} in Order ID {OrderId} by employee: {Email}", detail.GoldId, id, User.GetEmail());
                            TempData["Error"] = "Số lượng trong kho không đủ để xác nhận đơn hàng.";
                            return RedirectToAction("ManageOrders");
                        }
                    }

                    order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price); // Cập nhật tổng tiền
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Order {OrderId} approved by employee: {Email}", id, User.GetEmail());
                }
                else
                {
                    _logger.LogWarning("Order {OrderId} not found or not in Pending status for employee: {Email}", id, User.GetEmail());
                    TempData["Error"] = "Đơn hàng không tồn tại hoặc không ở trạng thái chờ xác nhận.";
                }
                return RedirectToAction("ManageOrders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order {OrderId} by employee: {Email}", id, User.GetEmail());
                TempData["Error"] = "Đã xảy ra lỗi khi xác nhận đơn hàng.";
                return RedirectToAction("ManageOrders");
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