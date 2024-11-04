namespace Entities.DataTransferObjects.User;

public class UserLite
{
    public string Email { get; set; }
    public bool IsAdmin { get; set; }
    public bool TwoFactorEnabled { get; set; }
}