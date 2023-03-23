using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects
{
    public class TwoFactorVerificationDto
    {
        public string Email { get; set; }
        public string Provider { get; set; }
        [Required(ErrorMessage = "Token is required")]
        public string TwoFactorToken { get; set; }
    }
}
