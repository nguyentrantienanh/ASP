namespace TienAnhGold.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // ID của người dùng (email hoặc tên)
        public string? EmployeeId { get; set; } // ID của nhân viên (nullable nếu chưa có nhân viên tham gia)
        public bool IsActive { get; set; } // Trạng thái đoạn chat
        public DateTime CreatedAt { get; set; } // Thời gian tạo
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>(); // Danh sách tin nhắn
        public bool HasAdminJoinedMessage { get; set; } = false; // Thêm cột này
        public bool UserEnded { get; set; } // Thêm cột để theo dõi trạng thái "Kết thúc" của User
        public bool EmployeeEnded { get; set; } // Thêm cột để theo dõi trạng thái "Kết thúc" của Employee
    }
}
