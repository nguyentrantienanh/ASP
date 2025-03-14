using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TienAnhGold.Data;
using TienAnhGold.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace TienAnhGold.Controllers
{
    public class OrderController : Controller
    {
        private readonly TienAnhGoldContext _context;

        public OrderController(TienAnhGoldContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách đơn hàng của người dùng
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem đơn hàng.";
                return RedirectToAction("Login", "User");
            }

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Gold)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Đặt hàng từ giỏ hàng
        [HttpPost]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> PlaceOrder()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để đặt hàng.";
                return RedirectToAction("Login", "User");
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Gold) // Include Gold to access Price
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn trống!";
                return RedirectToAction("Index", "Cart");
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = cartItems.Sum(c => c.Gold.Price * c.Quantity),
                OrderDetails = cartItems.Select(c => new OrderDetail
                {
                    GoldId = c.GoldId,
                    Quantity = c.Quantity,
                    Price = c.Gold.Price
                }).ToList(),
                CustomerName = "", // Đặt mặc định, sẽ cập nhật sau ở Checkout
                CustomerPhone = "",
                CustomerAddress = "",
                PaymentMethod = "", // Đặt mặc định, sẽ cập nhật sau ở Checkout
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems); // Xóa giỏ hàng sau khi đặt hàng
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Hủy đơn hàng
        [HttpPost]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> CancelOrder(int id, string reason)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để hủy đơn hàng.";
                return RedirectToAction("Login", "User");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null || order.Status != OrderStatus.Pending) // Chỉ hủy khi ở trạng thái Pending
            {
                TempData["Error"] = "Không thể hủy đơn hàng. Đơn hàng không tồn tại hoặc đã được xử lý.";
                return RedirectToAction("Index");
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đơn hàng đã bị hủy.";
            return RedirectToAction("Index");
        }

        // Quản lý đơn hàng cho Admin/Nhân viên
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ManageOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Gold)
                .ToListAsync(); // Hiển thị tất cả đơn hàng thay vì chỉ Pending

            return View(orders);
        }

        // Xác nhận đơn hàng
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken] // Thêm bảo vệ CSRF
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Gold)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("ManageOrders");
            }

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Đơn hàng không ở trạng thái chờ xác nhận.";
                return RedirectToAction("ManageOrders");
            }

            // Xử lý dựa trên phương thức thanh toán
            if (order.PaymentMethod == "BankTransfer")
            {
                order.Status = OrderStatus.Approved;
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
                        TempData["Error"] = "Số lượng trong kho không đủ để xác nhận đơn hàng.";
                        return RedirectToAction("ManageOrders");
                    }
                }
                TempData["Message"] = "Đơn hàng đã được xác nhận và tồn kho đã được cập nhật.";
            }
            else if (order.PaymentMethod == "COD")
            {
                order.Status = OrderStatus.AwaitingConfirmation;
                TempData["Message"] = "Đơn hàng đã được chuyển sang trạng thái chờ xác nhận nhận hàng.";
            }
            else
            {
                TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
                return RedirectToAction("ManageOrders");
            }

            order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.Price); // Cập nhật tổng tiền
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageOrders");
        }

        // Hoàn thành đơn hàng (Admin/Nhân viên xác nhận hoàn tất)
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken] // Thêm bảo vệ CSRF
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Gold)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("ManageOrders");
            }

            if (order.Status != OrderStatus.AwaitingConfirmation)
            {
                TempData["Error"] = "Chỉ có thể hoàn thành đơn hàng ở trạng thái chờ xác nhận thanh toán.";
                return RedirectToAction("ManageOrders");
            }

            // Giảm số lượng tồn kho cho COD khi hoàn thành
            if (order.PaymentMethod == "COD")
            {
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
                        TempData["Error"] = "Số lượng trong kho không đủ để hoàn thành đơn hàng.";
                        return RedirectToAction("ManageOrders");
                    }
                }
            }

            order.Status = OrderStatus.Completed;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đơn hàng đã hoàn thành.";
            return RedirectToAction("ManageOrders");
        }
    }
}