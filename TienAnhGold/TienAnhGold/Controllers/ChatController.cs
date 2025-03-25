using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TienAnhGold.Data;
using TienAnhGold.Models;
using Microsoft.EntityFrameworkCore;
using TienAnhGold.Hubs;

namespace TienAnhGold.Controllers
{
    public class ChatController : Controller
    {
        private readonly TienAnhGoldContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(TienAnhGoldContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View("Chat");
        }

        [HttpPost]
        public async Task<IActionResult> StartChat([FromBody] ChatRequest request)
        {
            try
            {
                Console.WriteLine($"StartChat called: userId={request.UserId}, role={request.Role}, targetId={request.TargetId}, targetRole={request.TargetRole}");

                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Role))
                {
                    return Json(new { success = false, error = "UserId hoặc Role không hợp lệ." });
                }

                Chat existingChat = null;

                if (request.Role == "User")
                {
                    existingChat = await _context.Chats
                        .FirstOrDefaultAsync(c => c.UserId == request.UserId);
                    if (existingChat != null)
                    {
                        if (!existingChat.IsActive)
                        {
                            existingChat.IsActive = true;
                            existingChat.HasAdminJoinedMessage = false;
                            existingChat.UserEnded = false;
                            existingChat.EmployeeEnded = false;
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"Reactivated chat for User: chatId={existingChat.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"Found existing active chat for User: chatId={existingChat.Id}");
                        }
                        return Json(new { success = true, chatId = existingChat.Id });
                    }

                    var newChat = new Chat
                    {
                        UserId = request.UserId,
                        EmployeeId = null,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        HasAdminJoinedMessage = false,
                        UserEnded = false,
                        EmployeeEnded = false
                    };
                    _context.Chats.Add(newChat);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created new chat for User: chatId={newChat.Id}, UserId={newChat.UserId}");
                    return Json(new { success = true, chatId = newChat.Id });
                }

                if (request.Role == "Employee")
                {
                    if (request.TargetRole == "User")
                    {
                        existingChat = await _context.Chats
                            .FirstOrDefaultAsync(c => c.UserId == request.TargetId);
                        if (existingChat != null)
                        {
                            if (string.IsNullOrEmpty(existingChat.EmployeeId))
                            {
                                existingChat.EmployeeId = request.UserId;
                                existingChat.IsActive = true;
                                existingChat.HasAdminJoinedMessage = false;
                                existingChat.UserEnded = false;
                                existingChat.EmployeeEnded = false;
                                await _context.SaveChangesAsync();
                                Console.WriteLine($"Updated EmployeeId and reactivated chatId={existingChat.Id}, EmployeeId={request.UserId}");
                            }
                            return Json(new { success = true, chatId = existingChat.Id });
                        }

                        var newChat = new Chat
                        {
                            UserId = request.TargetId,
                            EmployeeId = request.UserId,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            HasAdminJoinedMessage = false,
                            UserEnded = false,
                            EmployeeEnded = false
                        };
                        _context.Chats.Add(newChat);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Created new chat for Employee-User: chatId={newChat.Id}, UserId={newChat.UserId}, EmployeeId={newChat.EmployeeId}");
                        return Json(new { success = true, chatId = newChat.Id });
                    }
                    else if (request.TargetRole == "Admin")
                    {
                        existingChat = await _context.Chats
                            .FirstOrDefaultAsync(c => ((c.UserId == request.UserId && c.EmployeeId == request.TargetId) ||
                                                      (c.UserId == request.TargetId && c.EmployeeId == request.UserId)));
                        if (existingChat != null)
                        {
                            if (!existingChat.IsActive)
                            {
                                existingChat.IsActive = true;
                                existingChat.HasAdminJoinedMessage = false;
                                existingChat.UserEnded = false;
                                existingChat.EmployeeEnded = false;
                                await _context.SaveChangesAsync();
                                Console.WriteLine($"Reactivated chat between Employee and Admin: chatId={existingChat.Id}");
                            }
                            else
                            {
                                Console.WriteLine($"Found existing chat between Employee and Admin: chatId={existingChat.Id}");
                            }
                            return Json(new { success = true, chatId = existingChat.Id });
                        }

                        var newChat = new Chat
                        {
                            UserId = request.TargetId,
                            EmployeeId = request.UserId,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            HasAdminJoinedMessage = false,
                            UserEnded = false,
                            EmployeeEnded = false
                        };
                        _context.Chats.Add(newChat);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Created new chat for Employee-Admin: chatId={newChat.Id}, UserId={newChat.UserId}, EmployeeId={newChat.EmployeeId}");
                        return Json(new { success = true, chatId = newChat.Id });
                    }
                }

                if (request.Role == "Admin")
                {
                    existingChat = await _context.Chats
                        .FirstOrDefaultAsync(c => ((c.UserId == request.UserId && c.EmployeeId == request.TargetId) ||
                                                  (c.UserId == request.TargetId && c.EmployeeId == request.UserId)));
                    if (existingChat != null)
                    {
                        if (!existingChat.IsActive)
                        {
                            existingChat.IsActive = true;
                            existingChat.HasAdminJoinedMessage = false;
                            existingChat.UserEnded = false;
                            existingChat.EmployeeEnded = false;
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"Reactivated chat between Admin and Employee: chatId={existingChat.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"Found existing chat between Admin and Employee: chatId={existingChat.Id}");
                        }
                        return Json(new { success = true, chatId = existingChat.Id });
                    }

                    var newChat = new Chat
                    {
                        UserId = request.UserId,
                        EmployeeId = request.TargetId,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        HasAdminJoinedMessage = false,
                        UserEnded = false,
                        EmployeeEnded = false
                    };
                    _context.Chats.Add(newChat);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created new chat for Admin-Employee: chatId={newChat.Id}, UserId={newChat.UserId}, EmployeeId={newChat.EmployeeId}");
                    return Json(new { success = true, chatId = newChat.Id });
                }

                return Json(new { success = false, error = "Vai trò không hợp lệ." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StartChat: {ex.Message}");
                return Json(new { success = false, error = "Không thể khởi tạo chat: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EndChat([FromBody] EndChatRequest request)
        {
            try
            {
                var chat = await _context.Chats.FindAsync(request.ChatId);
                if (chat == null) return Json(new { success = false, error = "Đoạn chat không tồn tại." });

                if (request.UserRole == "User")
                {
                    chat.UserEnded = true;
                    Console.WriteLine($"User {request.UserId} ended the chat: chatId={request.ChatId}");
                }
                else if (request.UserRole == "Employee")
                {
                    chat.EmployeeEnded = true;
                    Console.WriteLine($"Employee {request.UserId} ended the chat: chatId={request.ChatId}");
                }
                else if (request.UserRole == "Admin")
                {
                    if (chat.UserId == request.UserId)
                    {
                        chat.UserEnded = true;
                        Console.WriteLine($"Admin {request.UserId} ended the chat: chatId={request.ChatId}");
                    }
                    else if (chat.EmployeeId == request.UserId)
                    {
                        chat.EmployeeEnded = true;
                        Console.WriteLine($"Admin {request.UserId} ended the chat: chatId={request.ChatId}");
                    }
                }

                await _context.SaveChangesAsync();

                if (chat.UserEnded && chat.EmployeeEnded)
                {
                    var messages = await _context.ChatMessages
                        .Where(m => m.ChatId == request.ChatId)
                        .ToListAsync();
                    if (messages.Any())
                    {
                        _context.ChatMessages.RemoveRange(messages);
                        Console.WriteLine($"Deleted {messages.Count} messages for chatId={request.ChatId}");
                    }

                    _context.Chats.Remove(chat);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Deleted chat: chatId={request.ChatId}");

                    // Gửi tín hiệu đến nhóm chat để thông báo rằng đoạn chat đã hoàn toàn kết thúc
                    await _hubContext.Clients.Group(request.ChatId.ToString()).SendAsync("ChatFullyEnded");
                }

                return Json(new { success = true, userId = request.UserId, userRole = request.UserRole });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EndChat: {ex.Message}");
                return Json(new { success = false, error = "Không thể kết thúc chat." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            try
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.ChatId == chatId)
                    .ToListAsync();
                Console.WriteLine($"Loaded messages for chatId={chatId}, count={messages.Count}");
                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMessages: {ex.Message}");
                return Json(new { success = false, error = "Không thể tải tin nhắn." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new { id = u.Email, name = u.Name })
                    .Distinct()
                    .ToListAsync();
                Console.WriteLine($"Loaded users: count={users.Count}");
                return Json(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUsers: {ex.Message}");
                return Json(new { success = false, error = "Không thể tải danh sách người dùng." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await _context.Employees
                    .Select(e => new { id = e.Email, name = e.Name })
                    .Distinct()
                    .ToListAsync();
                Console.WriteLine($"Loaded employees: count={employees.Count}");
                return Json(employees);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmployees: {ex.Message}");
                return Json(new { success = false, error = "Không thể tải danh sách nhân viên." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                var admins = await _context.Admins
                    .Select(a => new { id = a.Email, name = a.Name })
                    .Distinct()
                    .ToListAsync();
                Console.WriteLine($"Loaded admins: count={admins.Count}");
                return Json(admins);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAdmins: {ex.Message}");
                return Json(new { success = false, error = "Không thể tải danh sách admin." });
            }
        }
    }

    public class ChatRequest
    {
        public string UserId { get; set; }
        public string? TargetId { get; set; }
        public string Role { get; set; }
        public string? TargetRole { get; set; }
    }

    public class EndChatRequest
    {
        public int ChatId { get; set; }
        public string UserId { get; set; }
        public string UserRole { get; set; }
    }
}