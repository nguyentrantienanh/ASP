using ASP_thuchanh1.Data;
using Microsoft.EntityFrameworkCore;

namespace ASP_thuchanh1.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ASP_thuchanh1Context(
                serviceProvider.GetRequiredService<
                    DbContextOptions<ASP_thuchanh1Context>>()))
            {
                // Kiểm tra thông tin cuốn sách đã tồn tại hay chưa
                if (context.Book.Any())
                {
                    return;   // Không thêm nếu cuốn sách đã tồn tại trong DB
                }

                context.Book.AddRange(
                    new Book
                    {
                        Title = "Atomic Habits",
                        ReleaseDate = DateTime.Parse("2018-10-16"),
                        Genre = "Self-Help",
                        Price = 11.98M,
                        Rating = "R"
                    },
                    new Book
                    {
                        Title = "Dark Roads",
                        ReleaseDate = DateTime.Parse("2021-8-3"),
                        Genre = "Novel",
                        Price = 18.59M,
                        Rating = "R"
                    }
                );
                context.SaveChanges();//lưu dữ liệu
            }
        }
    }
}
