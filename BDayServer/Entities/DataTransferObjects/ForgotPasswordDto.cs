using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string ClientUri { get; set; }
    }
}
