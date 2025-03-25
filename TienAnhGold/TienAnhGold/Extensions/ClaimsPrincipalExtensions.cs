using System.Security.Claims;

namespace TienAnhGold.Extensions
{
    // Lớp mở rộng ClaimsPrincipalExtensions chứa phương thức mở rộng GetEmail
    public static class ClaimsPrincipalExtensions
    {
        public static string GetEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        }
    }
}