namespace TienAnhGold.Models
{
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(NewPassword) && NewPassword == ConfirmPassword;
        }
    }
}