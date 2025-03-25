namespace TienAnhGold.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatId { get; set; } // ID đoạn chat
        public string? SenderId { get; set; } // ID người gửi
        public string? SenderRole { get; set; } // Vai trò: "User", "Employee", "Admin"
        public string? Message { get; set; } // Nội dung tin nhắn
        public bool IsRead { get; set; } = false; // Mặc định là chưa đọc
        public DateTime SentAt { get; set; } // Thời gian gửi
    }
}
