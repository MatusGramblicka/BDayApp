namespace Entities.DataTransferObjects.User
{
    public class UserLite2StepsAuthDto
    {
        public string Email { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }
}
