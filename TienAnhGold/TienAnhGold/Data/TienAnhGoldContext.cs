using Microsoft.EntityFrameworkCore;
using TienAnhGold.Models;
using BCrypt.Net; // Đảm bảo gói BCrypt.Net-Next đã được cài đặt

namespace TienAnhGold.Data
{
    public class TienAnhGoldContext : DbContext
    {
        public TienAnhGoldContext(DbContextOptions<TienAnhGoldContext> options)
            : base(options)
        {
        }

        public DbSet<Gold> Gold { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<CartItem> CartItems { get; set; } = default!;
        public DbSet<Order> Orders { get; set; } = default!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = default!;
        public DbSet<Employee> Employees { get; set; } = default!;
        public DbSet<Admin> Admins { get; set; } = default!;
        public DbSet<Chat> Chats { get; set; } = default!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = default!;




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình cho thuộc tính Price trong Gold với độ chính xác 18 và tỷ lệ 2
            modelBuilder.Entity<Gold>()
                .Property(g => g.Price)
                .HasColumnType("decimal(18,2)"); // Chỉ định loại cột SQL Server với độ chính xác 18 và tỷ lệ 2

            // Cấu hình tên bảng cho tất cả các entity
            modelBuilder.Entity<Gold>().ToTable("Gold");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<CartItem>().ToTable("CartItems");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
            modelBuilder.Entity<Employee>().ToTable("Employees");
            modelBuilder.Entity<Admin>().ToTable("Admins");
 

            

            // Seed tài khoản admin duy nhất
            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    Id = 1,
                    Name = "Admin Master",
                    Email = "admin@tienanhgold.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Hash mật khẩu
                    Role = "Admin",
                    IsActive = true
                });
        }
    }
}