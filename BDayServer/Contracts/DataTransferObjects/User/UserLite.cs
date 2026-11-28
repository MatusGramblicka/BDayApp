using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.User;

public class UserLite
{
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public bool TwoFactorEnabled { get; set; }
}
