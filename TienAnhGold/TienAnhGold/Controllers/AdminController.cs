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

namespace TienAnhGold.Controllers
{
    // Xóa [Authorize(Roles = "Admin")] ở cấp controller để áp dụng quyền chi tiết hơn
    public class AdminController : Controller
    {
        private readonly TienAnhGoldContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(TienAnhGoldContext context, ILogger<AdminController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Admin/CreateAccount
        [Authorize(Roles = "Admin")]
        public IActionResult CreateAccount()
        {
            var model = new AccountViewModel();
            return View(model);
        }

        // POST: Admin/CreateAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAccount([Bind("Name,Email,Password,Role")] AccountViewModel model)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            if (model == null)
            {
                _logger.LogWarning("Model is null during CreateAccount POST by admin: {AdminEmail}", adminEmail);
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    if (await _context.Admins.AnyAsync(a => a.Email == model.Email) ||
                        await _context.Employees.AnyAsync(e => e.Email == model.Email) ||
                        await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        _logger.LogWarning("Email {Email} already in use during account creation by admin: {AdminEmail}", model.Email, adminEmail);
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        return View(model);
                    }

                    if (model.Role == "User")
                    {
                        var user = new User
                        {
                            Name = model.Name,
                            Email = model.Email,
                            Password = BCrypt.Net.BCrypt.HashPassword(model.Password ?? throw new ArgumentNullException(nameof(model.Password))),
                            Role = "User",
                            IsActive = true,
                            Address = ""
                        };

                        _context.Users.Add(user);
                        _logger.LogInformation("User {Email} created successfully by admin: {AdminEmail}", model.Email, adminEmail);
                    }
                    else if (model.Role == "Employee")
                    {
                        var employee = new Employee
                        {
                            Name = model.Name,
                            Email = model.Email,
                            Password = BCrypt.Net.BCrypt.HashPassword(model.Password ?? throw new ArgumentNullException(nameof(model.Password))),
                            Role = "Employee",
                            IsActive = true
                        };

                        _context.Employees.Add(employee);
                        _logger.LogInformation("Employee {Email} created successfully by admin: {AdminEmail}", model.Email, adminEmail);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Vai trò không hợp lệ. Vui lòng chọn User hoặc Employee.");
                        return View(model);
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Dashboard");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account for email: {Email} by admin: {AdminEmail}", model?.Email, adminEmail);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo tài khoản. Vui lòng thử lại.");
                return View(model);
            }
        }

        // GET: Admin/Login (Public cho phép đăng nhập)
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email && a.IsActive);
                if (admin == null)
                {
                    _logger.LogWarning("Admin not found for email: {Email}", email);
                    ModelState.AddModelError("", "Email không tồn tại hoặc tài khoản không hoạt động.");
                    return View();
                }

                if (!BCrypt.Net.BCrypt.Verify(password, admin.Password))
                {
                    _logger.LogWarning("Invalid password for email: {Email}", email);
                    ModelState.AddModelError("", "Mật khẩu không đúng.");
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.Name ?? string.Empty),
                    new Claim(ClaimTypes.Email, admin.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, "Admin")
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

                _logger.LogInformation("Admin {Email} logged in successfully.", email);
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login for email: {Email}", email);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.");
                return View();
            }
        }

        // GET: Admin/Logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("Admin logged out successfully.");
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin logout.");
                return RedirectToAction("Login");
            }
        }

        // GET: Admin/Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var dashboardViewModel = new DashboardViewModel
                {
                    UserCount = await _context.Users.CountAsync(),
                    EmployeeCount = await _context.Employees.CountAsync(),
                    OrderCount = await _context.Orders.CountAsync(), // Tổng số đơn hàng, không giới hạn trạng thái
                    ApprovedOrderCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Approved),
                    TotalRevenue = await _context.Orders
                        .Where(o => o.Status == OrderStatus.Completed)
                        .SumAsync(o => o.TotalAmount ?? 0),
                    GoldCount = await _context.Gold.CountAsync(),
                    RecentOrders = await _context.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .Take(5) // Lấy 5 đơn hàng gần đây
                        .ToListAsync()
                };

                _logger.LogInformation("Dashboard data for user: {Email} - " +
                    "UserCount: {UserCount}, EmployeeCount: {EmployeeCount}, " +
                    "OrderCount: {OrderCount}, ApprovedOrderCount: {ApprovedOrderCount}, " +
                    "TotalRevenue: {TotalRevenue}, GoldCount: {GoldCount}",
                    userEmail, dashboardViewModel.UserCount, dashboardViewModel.EmployeeCount,
                    dashboardViewModel.OrderCount, dashboardViewModel.ApprovedOrderCount,
                    dashboardViewModel.TotalRevenue, dashboardViewModel.GoldCount);

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user: {Email}", userEmail);
                return View(new DashboardViewModel());
            }
        }

        // GET: Admin/CreateEmployee
        [Authorize(Roles = "Admin")]
        public IActionResult CreateEmployee()
        {
            return View();
        }

        // POST: Admin/CreateEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEmployee([Bind("Name,Email,Password")] Employee employee)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                if (ModelState.IsValid)
                {
                    if (await _context.Admins.AnyAsync(a => a.Email == employee.Email) ||
                        await _context.Employees.AnyAsync(e => e.Email == employee.Email) ||
                        await _context.Users.AnyAsync(u => u.Email == employee.Email))
                    {
                        _logger.LogWarning("Email {Email} already in use during employee creation by admin: {AdminEmail}", employee.Email, adminEmail);
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        return View(employee);
                    }

                    employee.Password = BCrypt.Net.BCrypt.HashPassword(employee.Password);
                    employee.Role = "Employee";
                    employee.IsActive = true;

                    _context.Employees.Add(employee);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Employee {Email} created successfully by admin: {AdminEmail}", employee.Email, adminEmail);
                    return RedirectToAction("ManageEmployees");
                }
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee for email: {Email} by admin: {AdminEmail}", employee.Email, adminEmail);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo nhân viên. Vui lòng thử lại.");
                return View(employee);
            }
        }

        // GET: Admin/ManageEmployees
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageEmployees()
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
                _logger.LogInformation("Admin manage employees loaded successfully for user: {Email}", adminEmail);
                return View(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin manage employees for user: {Email}", adminEmail);
                return View(new List<Employee>());
            }
        }

        // GET: Admin/ManageUsers
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUsers()
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
                _logger.LogInformation("Admin manage users loaded successfully for user: {Email}", adminEmail);
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin manage users for user: {Email}", adminEmail);
                return View(new List<User>());
            }
        }

        // GET: Admin/EditUser
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(int id)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {Id} not found during edit by admin: {AdminEmail}", id, adminEmail);
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user with ID {Id} for edit by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageUsers");
            }
        }

        // POST: Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(int id, [Bind("Id,Name,Email,Address,Password,IsActive")] User user)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            if (id != user.Id)
            {
                _logger.LogWarning("ID mismatch during user edit by admin: {AdminEmail}", adminEmail);
                return BadRequest();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        _logger.LogWarning("User with ID {Id} not found during edit by admin: {AdminEmail}", id, adminEmail);
                        return NotFound();
                    }

                    existingUser.Name = user.Name;
                    existingUser.Email = user.Email;
                    existingUser.Address = user.Address;
                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        existingUser.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    }
                    existingUser.IsActive = user.IsActive;

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User with ID {Id} updated successfully by admin: {AdminEmail}", id, adminEmail);
                    return RedirectToAction("ManageUsers");
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {Id} by admin: {AdminEmail}", id, adminEmail);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật người dùng. Vui lòng thử lại.");
                return View(user);
            }
        }

        // GET: Admin/DeleteUser
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {Id} not found during deletion by admin: {AdminEmail}", id, adminEmail);
                    return NotFound("Người dùng không tồn tại.");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user with ID {Id} for deletion by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageUsers", new { error = "Đã xảy ra lỗi khi tải thông tin người dùng. Vui lòng thử lại." });
            }
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id, IFormCollection collection)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {Id} not found during deletion by admin: {AdminEmail}", id, adminEmail);
                    return NotFound("Người dùng không tồn tại.");
                }

                var relatedOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (relatedOrders)
                {
                    _logger.LogWarning("Cannot delete user with ID {Id} due to related orders by admin: {AdminEmail}", id, adminEmail);
                    return RedirectToAction("ManageUsers", new { error = "Không thể xóa người dùng vì có đơn hàng liên quan. Vui lòng xóa hoặc cập nhật các đơn hàng trước." });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User with ID {Id} deleted successfully by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageUsers");
            }
            catch (DbUpdateException ex)
            {
                var innerExceptionMessage = ex.InnerException?.Message ?? "No inner exception";
                _logger.LogError(ex, "Database error deleting user with ID {Id} by admin: {AdminEmail}. Inner Exception: {InnerExceptionMessage}", id, adminEmail, innerExceptionMessage);
                return RedirectToAction("ManageUsers", new { error = "Lỗi cơ sở dữ liệu khi xóa người dùng. Vui lòng kiểm tra lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {Id} by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageUsers", new { error = "Đã xảy ra lỗi khi xóa người dùng. Vui lòng thử lại." });
            }
        }

        // GET: Admin/ManageOrders
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ManageOrders()
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Where(o => o.Status != OrderStatus.Deleted)
                    .ToListAsync();
                _logger.LogInformation("Manage orders loaded successfully for user: {Email}", userEmail);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading manage orders for user: {Email}", userEmail);
                return View(new List<Order>());
            }
        }

        // GET: Admin/OrderDetails
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            if (id == 0)
            {
                _logger.LogWarning("Invalid Order ID: {Id} by user: {UserEmail}", id, userEmail);
                return NotFound();
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Order ID {Id} not found by user: {UserEmail}", id, userEmail);
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details for ID {Id} by user: {UserEmail}", id, userEmail);
                return NotFound();
            }
        }

        // AdminController.cs
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

                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.AwaitingConfirmation)
                {
                    _logger.LogWarning("Order ID {Id} is not in Pending status for confirmation by admin: {AdminEmail}. Current status: {Status}", id, adminEmail, order.Status);
                    return Json(new { success = false, message = "Chỉ có thể xác nhận đơn hàng ở trạng thái chờ xử lý." });
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
                    _logger.LogInformation("Reduced quantity for Gold ID {GoldId} to {NewQuantity} for Order ID {OrderId}", item.GoldId, item.Gold.Quantity, id);
                }

                // Cập nhật trạng thái và tổng tiền
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);
                order.Status = OrderStatus.Approved;
                order.ConfirmedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Order ID {Id} confirmed to Approved successfully by admin: {AdminEmail}", id, adminEmail);

                return Json(new { success = true, message = "Đơn hàng đã được xác nhận và tới bước thanh toán." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error confirming order ID {Id} by admin: {AdminEmail}", id, adminEmail);
                return Json(new { success = false, message = $"Lỗi cơ sở dữ liệu: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order ID {Id} by admin: {AdminEmail}", id, adminEmail);
                return Json(new { success = false, message = $"Đã xảy ra lỗi khi xác nhận đơn hàng: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> CompleteOrder(int id)
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
                    _logger.LogWarning("Order ID {Id} not found for completion by admin: {AdminEmail}", id, adminEmail);
                    return Json(new { success = false, message = "Đơn hàng không tồn tại." });
                }

                if (order.Status != OrderStatus.Approved)
                {
                    _logger.LogWarning("Order ID {Id} is not in Approved status for completion by admin: {AdminEmail}. Current status: {Status}", id, adminEmail, order.Status);
                    return Json(new { success = false, message = "Chỉ có thể hoàn thành đơn hàng ở trạng thái Approved." });
                }

                order.Status = OrderStatus.Completed;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order ID {Id} completed by admin: {AdminEmail}", id, adminEmail);

                return Json(new { success = true, message = "Đơn hàng đã hoàn thành." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order ID {Id} by admin: {AdminEmail}", id, adminEmail);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi hoàn thành đơn hàng." });
            }
        }
        // GET: Admin/EditEmployee
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditEmployee(int id)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {Id} not found during edit by admin: {AdminEmail}", id, adminEmail);
                    return NotFound();
                }
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee with ID {Id} for edit by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageEmployees");
            }
        }

        // POST: Admin/EditEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditEmployee(int id, [Bind("Id,Name,Email,Password,IsActive")] Employee employee)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            if (id != employee.Id)
            {
                _logger.LogWarning("ID mismatch during employee edit by admin: {AdminEmail}", adminEmail);
                return BadRequest();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingEmployee = await _context.Employees.FindAsync(id);
                    if (existingEmployee == null)
                    {
                        _logger.LogWarning("Employee with ID {Id} not found during edit by admin: {AdminEmail}", id, adminEmail);
                        return NotFound();
                    }

                    existingEmployee.Name = employee.Name;
                    existingEmployee.Email = employee.Email;
                    if (!string.IsNullOrEmpty(employee.Password))
                    {
                        existingEmployee.Password = BCrypt.Net.BCrypt.HashPassword(employee.Password);
                    }
                    existingEmployee.IsActive = employee.IsActive;

                    _context.Update(existingEmployee);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Employee with ID {Id} updated successfully by admin: {AdminEmail}", id, adminEmail);
                    return RedirectToAction("ManageEmployees");
                }
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {Id} by admin: {AdminEmail}", id, adminEmail);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật nhân viên. Vui lòng thử lại.");
                return View(employee);
            }
        }

        // GET: Admin/DeleteEmployee
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {Id} not found during deletion by admin: {AdminEmail}", id, adminEmail);
                    return NotFound("Nhân viên không tồn tại.");
                }

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee with ID {Id} for deletion by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageEmployees", new { error = "Đã xảy ra lỗi khi tải thông tin nhân viên. Vui lòng thử lại." });
            }
        }

        // POST: Admin/DeleteEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id, IFormCollection collection)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {Id} not found during deletion by admin: {AdminEmail}", id, adminEmail);
                    return NotFound("Nhân viên không tồn tại.");
                }

                var relatedOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (relatedOrders)
                {
                    _logger.LogWarning("Cannot delete employee with ID {Id} due to related orders by admin: {AdminEmail}", id, adminEmail);
                    return RedirectToAction("ManageEmployees", new { error = "Không thể xóa nhân viên vì có đơn hàng liên quan. Vui lòng xóa hoặc cập nhật các đơn hàng trước." });
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Employee with ID {Id} deleted successfully by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageEmployees");
            }
            catch (DbUpdateException ex)
            {
                var innerExceptionMessage = ex.InnerException?.Message ?? "No inner exception";
                _logger.LogError(ex, "Database error deleting employee with ID {Id} by admin: {AdminEmail}. Inner Exception: {InnerExceptionMessage}", id, adminEmail, innerExceptionMessage);
                return RedirectToAction("ManageEmployees", new { error = "Lỗi cơ sở dữ liệu khi xóa nhân viên. Vui lòng kiểm tra lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID {Id} by admin: {AdminEmail}", id, adminEmail);
                return RedirectToAction("ManageEmployees", new { error = "Đã xảy ra lỗi khi xóa nhân viên. Vui lòng thử lại." });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            string adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Order with ID {Id} not found during deletion by admin: {AdminEmail}", id, adminEmail);
                    return Json(new { success = false, message = "Đơn hàng không tồn tại." });
                }

                order.Status = OrderStatus.Deleted;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order with ID {Id} marked as deleted by admin: {AdminEmail}", id, adminEmail);

                return Json(new { success = true, message = "Đơn hàng đã được đánh dấu là xóa." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order with ID {Id} by admin: {AdminEmail}", id, adminEmail);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa đơn hàng." });
            }
        }
    }
}