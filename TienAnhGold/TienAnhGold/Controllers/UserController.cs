using Microsoft.AspNetCore.Mvc;
using TienAnhGold.Models;
using TienAnhGold.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace TienAnhGold.Controllers
{
    public class UserController : Controller
    {
        private readonly TienAnhGoldContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(TienAnhGoldContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: User/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: User/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Name,Email,Password,Address")] User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (await _context.Admins.AnyAsync(a => a.Email == user.Email) ||
                        await _context.Employees.AnyAsync(e => e.Email == user.Email) ||
                        await _context.Users.AnyAsync(u => u.Email == user.Email))
                    {
                        _logger.LogWarning("Email {Email} already in use during registration.", user.Email);
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        return View(user);
                    }

                    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    user.Role = "User";
                    user.IsActive = true;

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User {Email} registered successfully.", user.Email);
                    return RedirectToAction("Login", "User");
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", user.Email);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại.");
                return View(user);
            }
        }

        // GET: User/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: User/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                // Kiểm tra tài khoản admin trước
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email && a.IsActive);
                if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, admin.Name),
                        new Claim(ClaimTypes.Email, admin.Email),
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
                    return RedirectToAction("Dashboard", "Admin");
                }

                // Kiểm tra tài khoản employee
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email && e.IsActive);
                if (employee != null && BCrypt.Net.BCrypt.Verify(password, employee.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, employee.Name),
                        new Claim(ClaimTypes.Email, employee.Email),
                        new Claim(ClaimTypes.Role, "Employee")
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

                    HttpContext.Session.SetInt32("UserId", employee.Id);
                    _logger.LogInformation("Employee {Email} logged in successfully with Employee role.", email);
                    return RedirectToAction("Dashboard", "Employee");
                }

                // Kiểm tra tài khoản người dùng
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    _logger.LogWarning("Invalid login attempt for email: {Email}", email);
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                    return View();
                }

                var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // Lưu userId vào claims
                };

                var userClaimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var userAuthProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(userClaimsIdentity),
                    userAuthProperties);

                HttpContext.Session.SetInt32("UserId", user.Id);
                _logger.LogInformation("User {Email} logged in successfully.", email);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", email);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.");
                return View();
            }
        }

        // GET: User/Logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Remove("UserId");
                _logger.LogInformation("User logged out successfully.");
                return RedirectToAction("Login", "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user logout.");
                return RedirectToAction("Login", "User");
            }
        }

        // GET: User/Profile
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("User not authenticated or UserId not found in session.");
                    return Unauthorized();
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User with ID {UserId} not found in database.", userId);
                    return NotFound();
                }

                _logger.LogInformation("User profile loaded successfully for user: {Email}", user.Email);
                return View(user);
            }
            catch (Exception ex)
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                _logger.LogError(ex, "Error loading user profile for UserId: {UserId}", userId ?? 0);
                return NotFound();
            }
        }

        // GET: User/EditProfile
        [Authorize(Roles = "User")]
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("User not authenticated or UserId not found in session.");
                    return Unauthorized();
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User with ID {UserId} not found in database.", userId);
                    return NotFound();
                }

                _logger.LogInformation("User edit profile loaded successfully for user: {Email}", user.Email);
                return View(user);
            }
            catch (Exception ex)
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                _logger.LogError(ex, "Error loading user edit profile for UserId: {UserId}", userId ?? 0);
                return NotFound();
            }
        }

        // POST: User/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> EditProfile([Bind("Id,Name,Email,Password,Address")] User user, string OldPassword)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("User not authenticated or UserId not found in session during edit profile.");
                    return Unauthorized();
                }

                if (ModelState.IsValid)
                {
                    var existingUser = await _context.Users.FindAsync(user.Id);
                    if (existingUser == null)
                    {
                        _logger.LogError("User with ID {UserId} not found in database during edit profile.", user.Id);
                        return NotFound();
                    }

                    if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != user.Id))
                    {
                        _logger.LogWarning("Email {Email} already in use during profile update for user ID: {UserId}", user.Email, user.Id);
                        ModelState.AddModelError("Email", "Email đã được sử dụng bởi tài khoản khác.");
                        return View(user);
                    }

                    existingUser.Name = user.Name;
                    existingUser.Email = user.Email;
                    existingUser.Address = user.Address;

                    if (!string.IsNullOrEmpty(user.Password) && !string.IsNullOrEmpty(OldPassword))
                    {
                        if (!BCrypt.Net.BCrypt.Verify(OldPassword, existingUser.Password))
                        {
                            _logger.LogWarning("Invalid old password for user: {Email} during profile update.", existingUser.Email);
                            ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng.");
                            return View(user);
                        }
                        existingUser.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    }
                    else if (!string.IsNullOrEmpty(user.Password) && string.IsNullOrEmpty(OldPassword))
                    {
                        _logger.LogWarning("Old password required to change password for user: {Email}.", existingUser.Email);
                        ModelState.AddModelError("OldPassword", "Vui lòng nhập mật khẩu cũ để thay đổi mật khẩu mới.");
                        return View(user);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User profile updated successfully for user: {Email}", existingUser.Email);
                    return RedirectToAction("Profile");
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for UserId: {UserId}", user.Id);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật thông tin. Vui lòng thử lại.");
                return View(user);
            }
        }

        // GET: User/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: User/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email} during forgot password.", email);
                    ModelState.AddModelError("", "Email không tồn tại hoặc tài khoản không hoạt động.");
                    return View();
                }

                _logger.LogInformation("User requested password recovery for email: {Email}, instructed to contact admin for modification.", email);
                return RedirectToAction("ContactAdminForPasswordModification", new { email = email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email: {Email}", email);
                ModelState.AddModelError("", "Đã xảy ra lỗi. Vui lòng thử lại hoặc liên hệ hỗ trợ.");
                return View();
            }
        }

        // GET: User/ContactAdminForPasswordModification
        public IActionResult ContactAdminForPasswordModification(string email)
        {
            ViewBag.Email = email;
            return View();
        }

  

        // GET: User/ResetPasswordConfirmation
        public IActionResult ResetPasswordConfirmation(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        // GET: User/DeleteAccount
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("User not authenticated or UserId not found in session for delete account.");
                    return Unauthorized();
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User with ID {UserId} not found in database for delete account.", userId);
                    return NotFound();
                }

                _logger.LogInformation("User delete account form loaded successfully for user: {Email}", user.Email);
                return View(user);
            }
            catch (Exception ex)
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                _logger.LogError(ex, "Error loading delete account form for UserId: {UserId}", userId ?? 0);
                return NotFound();
            }
        }

        // POST: User/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteAccount(int id, string Password)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("User not authenticated or UserId not found in session for delete account.");
                    return Unauthorized();
                }

                if (id != userId)
                {
                    _logger.LogWarning("Invalid user ID {Id} for delete account by UserId: {UserId}", id, userId);
                    return BadRequest("ID người dùng không hợp lệ.");
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogError("User with ID {UserId} not found in database for delete account.", id);
                    return NotFound();
                }

                if (string.IsNullOrEmpty(Password) || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
                {
                    _logger.LogWarning("Invalid password for user: {Email} during delete account.", user.Email);
                    ModelState.AddModelError("Password", "Mật khẩu không đúng. Vui lòng nhập mật khẩu chính xác để xóa tài khoản.");
                    return View(user);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User account deleted successfully for user: {Email}", user.Email);

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Remove("UserId");
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user account for UserId: {UserId}", id);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xóa tài khoản. Vui lòng thử lại.");
                return View();
            }
        }

        // Chuyển các action liên quan đến giỏ hàng, thanh toán, đơn hàng từ HomeController sang đây

        private async Task<Order> GetCartAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userIdInt = null;
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int parsedUserId))
            {
                userIdInt = parsedUserId;
            }

            if (userIdInt.HasValue)
            {
                // Nếu đã đăng nhập, lấy giỏ hàng từ database
                var cartOrder = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.UserId == userIdInt && o.Status == OrderStatus.Pending);

                if (cartOrder == null)
                {
                    cartOrder = new Order
                    {
                        UserId = userIdInt.Value,
                        OrderDate = DateTime.Now,
                        Status = OrderStatus.Pending,
                        OrderDetails = new List<OrderDetail>()
                    };
                    _context.Orders.Add(cartOrder);
                    await _context.SaveChangesAsync();
                }
                return cartOrder;
            }
            else
            {
                // Nếu chưa đăng nhập, lấy giỏ hàng từ session
                var tempCartJson = HttpContext.Session.GetString("TempCart");
                if (string.IsNullOrEmpty(tempCartJson))
                {
                    return new Order
                    {
                        OrderDate = DateTime.Now,
                        Status = OrderStatus.Pending,
                        OrderDetails = new List<OrderDetail>()
                    };
                }
                return System.Text.Json.JsonSerializer.Deserialize<Order>(tempCartJson) ?? new Order
                {
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    OrderDetails = new List<OrderDetail>()
                };
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int id, int quantity = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userIdInt = null;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int parsedUserId))
                {
                    userIdInt = parsedUserId;
                }

                var gold = await _context.Gold.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
                if (gold == null)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm với ID {Id} để mua ngay.", id);
                    return Json(new { success = false, message = $"Sản phẩm với ID {id} không tồn tại." });
                }

                if (gold.Quantity < quantity)
                {
                    _logger.LogWarning("Số lượng trong kho không đủ cho sản phẩm ID {Id}. Yêu cầu: {Quantity}, Sẵn có: {Available}", id, quantity, gold.Quantity);
                    return Json(new { success = false, message = $"Số lượng trong kho không đủ. Yêu cầu: {quantity}, Sẵn có: {gold.Quantity}." });
                }

                if (!userIdInt.HasValue)
                {
                    _logger.LogWarning("Người dùng chưa đăng nhập khi cố gắng mua ngay sản phẩm ID {Id}.", id);
                    return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục mua hàng.", redirectUrl = Url.Action("Login", "User") });
                }

                var cartOrder = await GetCartAsync();
                cartOrder.OrderDetails.Clear();
                cartOrder.OrderDetails.Add(new OrderDetail
                {
                    GoldId = id,
                    Quantity = quantity,
                    Price = gold.Price
                });
                cartOrder.TotalAmount = cartOrder.OrderDetails.Sum(od => od.Quantity * od.Price);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã thêm sản phẩm ID {Id} vào giỏ hàng để mua ngay cho UserId {UserId}.", id, userIdInt);

                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm để mua ngay.",
                    redirectUrl = Url.Action("Checkout", "User")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thực hiện mua ngay sản phẩm ID {Id}.", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi thực hiện mua ngay: " + ex.Message });
            }
        }
        public async Task<IActionResult> Cart()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) && HttpContext.Session.GetString("TempCart") == null)
                {
                    _logger.LogWarning("Người dùng chưa đăng nhập và không có giỏ hàng tạm trong session.");
                    TempData["Error"] = "Vui lòng thêm sản phẩm vào giỏ hàng hoặc đăng nhập để xem giỏ hàng.";
                    return View(new Order());
                }

                var cartOrder = await GetCartAsync();
                var goldList = await _context.Gold.ToListAsync();
                ViewData["GoldList"] = goldList;
                ViewBag.CartCount = cartOrder.OrderDetails.Sum(od => od.Quantity);
                return View(cartOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất giỏ hàng.");
                TempData["Error"] = "Đã xảy ra lỗi khi truy xuất giỏ hàng: " + ex.Message;
                ViewBag.CartCount = 0;
                return View(new Order());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userIdInt = null;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int parsedUserId))
                {
                    userIdInt = parsedUserId;
                }

                var gold = await _context.Gold.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
                if (gold == null)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm với ID {Id} để thêm vào giỏ hàng.", id);
                    return Json(new { success = false, message = $"Sản phẩm với ID {id} không tồn tại." });
                }

                if (gold.Quantity < quantity)
                {
                    _logger.LogWarning("Số lượng trong kho không đủ cho sản phẩm ID {Id}. Yêu cầu: {Quantity}, Sẵn có: {Available}", id, quantity, gold.Quantity);
                    return Json(new { success = false, message = $"Số lượng trong kho không đủ. Yêu cầu: {quantity}, Sẵn có: {gold.Quantity}." });
                }

                var cartOrder = await GetCartAsync();
                var existingDetail = cartOrder.OrderDetails.FirstOrDefault(od => od.GoldId == id);

                if (existingDetail != null)
                {
                    existingDetail.Quantity += quantity;
                    _logger.LogInformation("Tăng số lượng cho sản phẩm ID {Id} trong giỏ hàng. Số lượng mới: {Quantity}", id, existingDetail.Quantity);
                }
                else
                {
                    cartOrder.OrderDetails.Add(new OrderDetail
                    {
                        GoldId = id,
                        Quantity = quantity,
                        Price = gold.Price
                    });
                    _logger.LogInformation("Thêm sản phẩm mới ID {Id} vào giỏ hàng. Số lượng: {Quantity}", id, quantity);
                }

                cartOrder.TotalAmount = cartOrder.OrderDetails.Sum(od => od.Quantity * od.Price);

                if (!userIdInt.HasValue)
                {
                    HttpContext.Session.SetString("TempCart", System.Text.Json.JsonSerializer.Serialize(cartOrder));
                    _logger.LogInformation("Lưu giỏ hàng vào session cho người dùng chưa đăng nhập.");
                }
                else
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đã thêm sản phẩm ID {Id} vào giỏ hàng cho UserId {UserId}. Tổng số mặt hàng: {TotalItems}", id, userIdInt, cartOrder.OrderDetails.Count);
                }

                var redirectUrl = Url.Action("Cart", "User");
                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng.",
                    redirectUrl = redirectUrl,
                    cartCount = cartOrder.OrderDetails.Sum(od => od.Quantity)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm sản phẩm ID {Id} vào giỏ hàng.", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi thêm sản phẩm vào giỏ hàng: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userIdInt = null;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int parsedUserId))
                {
                    userIdInt = parsedUserId;
                }

                var cartOrder = await GetCartAsync();
                var detailToRemove = cartOrder.OrderDetails.FirstOrDefault(od => od.GoldId == id);

                if (detailToRemove != null)
                {
                    cartOrder.OrderDetails.Remove(detailToRemove);
                    cartOrder.TotalAmount = cartOrder.OrderDetails.Sum(od => od.Quantity * od.Price);

                    if (!userIdInt.HasValue)
                    {
                        HttpContext.Session.SetString("TempCart", System.Text.Json.JsonSerializer.Serialize(cartOrder));
                    }
                    else
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Đã xóa sản phẩm ID {Id} khỏi giỏ hàng cho UserId {UserId}.", id, cartOrder.UserId);
                    }
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng.", redirectUrl = Url.Action("Cart", "User") });
                }
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm ID {Id} khỏi giỏ hàng.", id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa sản phẩm khỏi giỏ hàng." });
                }
                return RedirectToAction("Cart", new { error = "Đã xảy ra lỗi khi xóa sản phẩm khỏi giỏ hàng." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userIdInt = null;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int parsedUserId))
                {
                    userIdInt = parsedUserId;
                }

                var cartOrder = await GetCartAsync();
                if (!userIdInt.HasValue)
                {
                    HttpContext.Session.Remove("TempCart");
                    _logger.LogInformation("Đã xóa giỏ hàng tạm từ session.");
                }
                else
                {
                    _context.OrderDetails.RemoveRange(cartOrder.OrderDetails);
                    _context.Orders.Remove(cartOrder);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đã xóa giỏ hàng cho UserId {UserId}.", cartOrder.UserId);
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng.", redirectUrl = Url.Action("Cart", "User") });
                }
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa giỏ hàng.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa giỏ hàng." });
                }
                return RedirectToAction("Cart", new { error = "Đã xảy ra lỗi khi xóa giỏ hàng." });
            }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cartOrder = await GetCartAsync();
                if (!cartOrder.OrderDetails.Any())
                {
                    _logger.LogWarning("Giỏ hàng trống khi thanh toán cho UserId {UserId}.", cartOrder.UserId);
                    return RedirectToAction("Cart", new { error = "Giỏ hàng trống." });
                }
                return View(cartOrder);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Người dùng chưa xác thực để thanh toán.");
                return RedirectToAction("Login", "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất giỏ hàng để thanh toán.");
                return RedirectToAction("Cart", new { error = "Đã xảy ra lỗi khi truy cập trang thanh toán." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Checkout(Order model)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning("UserId không hợp lệ để thanh toán.");
                    return RedirectToAction("Cart", new { error = "Không thể xác định người dùng. Vui lòng thử lại." });
                }

                var cartOrder = await GetCartAsync();
                if (!cartOrder.OrderDetails.Any())
                {
                    _logger.LogWarning("Giỏ hàng trống khi thanh toán cho UserId {UserId}.", cartOrder.UserId);
                    return RedirectToAction("Cart", new { error = "Giỏ hàng trống." });
                }

                // Cập nhật thông tin đơn hàng từ model
                cartOrder.CustomerName = model.CustomerName;
                cartOrder.CustomerPhone = model.CustomerPhone;
                cartOrder.CustomerAddress = model.CustomerAddress;
                cartOrder.PaymentMethod = model.PaymentMethod; // Lấy từ form
                cartOrder.TotalAmount = cartOrder.OrderDetails.Sum(od => od.Quantity * od.Price);
                cartOrder.Status = model.PaymentMethod.ToLower() == "cod" ? OrderStatus.Pending : OrderStatus.AwaitingConfirmation; // Thêm ToLower để tránh lỗi hoa/thường
                cartOrder.OrderDate = DateTime.Now;
                cartOrder.UserId = userIdInt; // Đảm bảo gán UserId

                // Lưu đơn hàng vào cơ sở dữ liệu
                await _context.SaveChangesAsync();

                // Làm trống giỏ hàng bằng cách xóa các OrderDetails, nhưng giữ lại bản ghi Orders
                _context.OrderDetails.RemoveRange(cartOrder.OrderDetails);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đơn hàng đã được gửi cho UserId {UserId}. OrderId: {OrderId}, Phương thức thanh toán: {PaymentMethod}, Trạng thái: {Status}",
                    userIdInt, cartOrder.Id, cartOrder.PaymentMethod, cartOrder.Status);

                TempData["OrderNotification"] = "Đặt hàng thành công!";
                return RedirectToAction("Orders");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Thao tác không hợp lệ trong quá trình thanh toán.");
                return RedirectToAction("Cart", new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình thanh toán cho UserId: {UserId}.", User);
                return RedirectToAction("Cart", new { error = "Đã xảy ra lỗi khi thanh toán. Vui lòng thử lại." });
            }
        }
        // UserController.cs
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Orders()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning("UserId không hợp lệ để xem đơn hàng.");
                    return View(new List<Order>());
                }

                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .Where(o => o.UserId == userIdInt)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                // Kiểm tra đơn hàng mới được xác nhận trong 5 phút
                var recentlyApproved = orders.Any(o => o.Status == OrderStatus.Approved && o.OrderDate > DateTime.Now.AddMinutes(-5));
                if (recentlyApproved)
                {
                    TempData["OrderMessage"] = "Một hoặc nhiều đơn hàng của bạn đã được xác nhận. Chờ giao hàng!";
                }

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất đơn hàng của người dùng.");
                return View(new List<Order>());
            }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            if (id == 0)
            {
                _logger.LogWarning("ID đơn hàng không hợp lệ: {Id}.", id);
                return NotFound();
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning("UserId không hợp lệ để xem chi tiết đơn hàng.");
                    return NotFound();
                }

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userIdInt);

                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy đơn hàng với ID {Id} hoặc không thuộc về người dùng.", id);
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất chi tiết đơn hàng với ID {Id}.", id);
                return NotFound();
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .Where(o => o.Status != OrderStatus.Completed)
                    .OrderByDescending(o => o.OrderDate)
                    .AsNoTracking()
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất đơn hàng admin.");
                return View(new List<Order>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Gold)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy đơn hàng với ID {Id} để phê duyệt.", id);
                    return Json(new { success = false, message = "Đơn hàng không tồn tại." });
                }

                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Approved;
                    order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price);

                    foreach (var detail in order.OrderDetails)
                    {
                        var gold = await _context.Gold.FindAsync(detail.GoldId);
                        if (gold != null && gold.Quantity >= detail.Quantity)
                        {
                            gold.Quantity -= detail.Quantity;
                            if (gold.Quantity < 0) gold.Quantity = 0;
                        }
                        else
                        {
                            _logger.LogWarning("Số lượng trong kho không đủ cho Gold ID {GoldId} trong Order ID {OrderId}.", detail.GoldId, id);
                            return Json(new { success = false, message = "Số lượng trong kho không đủ để duyệt đơn hàng." });
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đơn hàng ID {Id} đã được admin phê duyệt.", id);
                }
                else
                {
                    return Json(new { success = false, message = "Đơn hàng không ở trạng thái chờ xử lý." });
                }

                return Json(new { success = true, message = "Đơn hàng đã được duyệt." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phê duyệt đơn hàng ID {Id}.", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi duyệt đơn hàng." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning("Người dùng chưa xác thực hoặc UserId không hợp lệ để xác nhận thanh toán.");
                    return Json(new { success = false, message = "Vui lòng đăng nhập để xác nhận thanh toán.", redirectUrl = Url.Action("Login", "User") });
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userIdInt);

                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy đơn hàng với ID {Id} hoặc không thuộc về người dùng.", id);
                    return Json(new { success = false, message = "Đơn hàng không tồn tại hoặc không thuộc về bạn." });
                }

                if (order.Status == OrderStatus.Approved && order.PaymentMethod == "COD")
                {
                    order.Status = OrderStatus.AwaitingConfirmation;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("UserId {UserId} đã xác nhận thanh toán COD cho OrderId {OrderId}.", userIdInt, id);
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xác nhận thanh toán ở trạng thái hiện tại." });
                }

                return Json(new { success = true, message = "Đã gửi yêu cầu xác nhận thanh toán. Vui lòng chờ admin xử lý.", redirectUrl = Url.Action("OrderDetails", "User", new { id = id }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác nhận thanh toán cho đơn hàng ID {Id}.", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xác nhận thanh toán." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy đơn hàng với ID {Id} để hoàn thành.", id);
                    return Json(new { success = false, message = "Đơn hàng không tồn tại." });
                }

                if (order.Status == OrderStatus.AwaitingConfirmation)
                {
                    order.Status = OrderStatus.Completed;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đơn hàng ID {Id} đã được admin hoàn thành.", id);
                }
                else
                {
                    return Json(new { success = false, message = "Chỉ có thể hoàn thành đơn hàng ở trạng thái chờ xác nhận thanh toán." });
                }

                return Json(new { success = true, message = "Đơn hàng đã hoàn thành." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hoàn thành đơn hàng ID {Id}.", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi hoàn thành đơn hàng." });
            }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetOrderStatuses()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning("UserId không hợp lệ để lấy trạng thái đơn hàng.");
                    return Json(new List<object>());
                }

                // Lấy dữ liệu thô từ cơ sở dữ liệu
                var orders = await _context.Orders
                    .Where(o => o.UserId == userIdInt)
                    .Select(o => new { Id = o.Id, Status = o.Status })
                    .ToListAsync();

                // Ánh xạ trạng thái trong bộ nhớ
                var result = orders.Select(o => new
                {
                    id = o.Id,
                    statusText = MapStatusToText(o.Status),
                    statusClass = MapStatusToClass(o.Status)
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy trạng thái đơn hàng cho UserId: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return Json(new List<object>());
            }
        }

        // Phương thức ánh xạ trạng thái thành text
        private string MapStatusToText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ duyệt",
                OrderStatus.Approved => "Đã xác nhận - Chờ giao hàng",
                OrderStatus.AwaitingConfirmation => "Chờ xác nhận nhận hàng",
                OrderStatus.Completed => "Hoàn thành",
                _ => "Không xác định"
            };
        }

        // Phương thức ánh xạ trạng thái thành class
        private string MapStatusToClass(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "warning",
                OrderStatus.Approved => "info",
                OrderStatus.AwaitingConfirmation => "primary",
                OrderStatus.Completed => "success",
                _ => "secondary"
            };
        }


        // POST: User/DeleteOrder

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                _logger.LogWarning("Invalid UserId for deleting order.");
                return Unauthorized();
            }

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userIdInt && o.Status == OrderStatus.Deleted);

                if (order == null)
                {
                    _logger.LogWarning("Order with ID {Id} not found or not deleted for UserId {UserId}", id, userIdInt);
                    TempData["OrderNotification"] = "Đơn hàng không tồn tại hoặc không thể xóa.";
                    return RedirectToAction("Orders");
                }

                // Xóa hoàn toàn đơn hàng khỏi cơ sở dữ liệu
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order with ID {Id} permanently deleted by UserId {UserId}", id, userIdInt);

                TempData["OrderNotification"] = "Đơn hàng đã được xóa vĩnh viễn.";
                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order with ID {Id} by UserId {UserId}", id, userId);
                TempData["OrderNotification"] = "Đã xảy ra lỗi khi xóa đơn hàng.";
                return RedirectToAction("Orders");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteOrderPermanently(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                _logger.LogWarning("Invalid UserId for deleting order.");
                return Unauthorized();
            }

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userIdInt && o.Status == OrderStatus.Deleted);

                if (order == null)
                {
                    _logger.LogWarning("Order with ID {Id} not found or not deleted for UserId {UserId}", id, userIdInt);
                    TempData["OrderNotification"] = "Đơn hàng không tồn tại hoặc không thể xóa.";
                    return RedirectToAction("Orders");
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order with ID {Id} permanently deleted by UserId {UserId}", id, userIdInt);

                TempData["OrderNotification"] = "Đơn hàng đã được xóa vĩnh viễn.";
                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order with ID {Id} by UserId {UserId}", id, userId);
                TempData["OrderNotification"] = "Đã xảy ra lỗi khi xóa đơn hàng.";
                return RedirectToAction("Orders");
            }
        }
    }
}