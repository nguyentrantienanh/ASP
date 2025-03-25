using Microsoft.AspNetCore.Mvc;
using TienAnhGold.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TienAnhGold.Data;

namespace TienAnhGold.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TienAnhGoldContext _context;

        public HomeController(ILogger<HomeController> logger, TienAnhGoldContext context)
        {
            _logger = logger;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index(string searchString, string materialFilter, string sortOrder)
        {
            try
            {
                var golds = _context.Gold.AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    string searchLower = searchString.ToLower();
                    golds = golds.Where(g => g.Name.ToLower().Contains(searchLower));
                    _logger.LogInformation("Áp dụng bộ lọc tìm kiếm GREP: {SearchString}", searchString);
                }

                if (!string.IsNullOrEmpty(materialFilter))
                {
                    golds = golds.Where(g => g.Material == materialFilter);
                    _logger.LogInformation("Áp dụng bộ lọc chất liệu: {MaterialFilter}", materialFilter);
                }

                switch (sortOrder)
                {
                    case "priceAsc":
                        golds = golds.OrderBy(g => g.Price);
                        _logger.LogInformation("Sắp xếp theo giá tăng dần");
                        break;
                    case "priceDesc":
                        golds = golds.OrderByDescending(g => g.Price);
                        _logger.LogInformation("Sắp xếp theo giá giảm dần");
                        break;
                    default:
                        _logger.LogInformation("Không áp dụng sắp xếp");
                        break;
                }

                var products = await golds.ToListAsync();

                if (products == null || !products.Any())
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm nào sau khi áp dụng bộ lọc.");
                    return View(new List<Gold>());
                }

                foreach (var product in products)
                {
                    _logger.LogInformation("Sản phẩm {Id} Tên: {Name}, Đường dẫn ảnh: {ImagePath}, Chất liệu: {Material}, Giá: {Price}",
                        product.Id, product.Name, product.ImagePath, product.Material, product.Price);
                }

                ViewData["CurrentSearch"] = searchString;
                ViewData["CurrentMaterial"] = materialFilter;
                ViewData["CurrentSort"] = sortOrder;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất sản phẩm cho trang Index với các bộ lọc - Tìm kiếm: {SearchString}, Chất liệu: {MaterialFilter}, Sắp xếp: {SortOrder}",
                    searchString, materialFilter, sortOrder);
                return View(new List<Gold>());
            }
        }

        public async Task<IActionResult> Privacy(string searchString, string materialFilter, string sortOrder)
        {
            try
            {
                var golds = _context.Gold.AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    string searchLower = searchString.ToLower();
                    golds = golds.Where(g => g.Name.ToLower().Contains(searchLower));
                    _logger.LogInformation("Áp dụng bộ lọc tìm kiếm GREP: {SearchString}", searchString);
                }

                if (!string.IsNullOrEmpty(materialFilter))
                {
                    golds = golds.Where(g => g.Material == materialFilter);
                    _logger.LogInformation("Áp dụng bộ lọc chất liệu: {MaterialFilter}", materialFilter);
                }

                switch (sortOrder)
                {
                    case "priceAsc":
                        golds = golds.OrderBy(g => g.Price);
                        _logger.LogInformation("Sắp xếp theo giá tăng dần trong Privacy");
                        break;
                    case "priceDesc":
                        golds = golds.OrderByDescending(g => g.Price);
                        _logger.LogInformation("Sắp xếp theo giá giảm dần trong Privacy");
                        break;
                    default:
                        _logger.LogInformation("Không áp dụng sắp xếp trong Privacy");
                        break;
                }

                var products = await golds.ToListAsync();

                if (products == null || !products.Any())
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm nào sau khi áp dụng bộ lọc trong Privacy.");
                    return View(new List<Gold>());
                }

                foreach (var product in products)
                {
                    _logger.LogInformation("Sản phẩm {Id} Tên: {Name}, Đường dẫn ảnh: {ImagePath}, Chất liệu: {Material}, Giá: {Price}",
                        product.Id, product.Name, product.ImagePath, product.Material, product.Price);
                }

                ViewData["CurrentSearch"] = searchString;
                ViewData["CurrentMaterial"] = materialFilter;
                ViewData["CurrentSort"] = sortOrder;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất sản phẩm cho trang Privacy với các bộ lọc - Tìm kiếm: {SearchString}, Chất liệu: {MaterialFilter}, Sắp xếp: {SortOrder}",
                    searchString, materialFilter, sortOrder);
                return View(new List<Gold>());
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
            {
                _logger.LogWarning("ID sản phẩm không hợp lệ được cung cấp cho hành động Details.");
                return NotFound();
            }

            try
            {
                var gold = await _context.Gold.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
                if (gold == null)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm với ID {Id}.", id);
                    return NotFound();
                }

                return View(gold);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất chi tiết sản phẩm với ID {Id}.", id);
                return NotFound();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Đã xảy ra lỗi. RequestId: {RequestId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult contact()
        {
            return View();
        }

        public IActionResult Introduce()
        {
            return View();
        }
    }

}