namespace Entities.DataTransferObjects
{
    public class AuthTokenDto
    {
        public bool? IsAuthSuccessful { get; set; }
        public string Token { get; set; }
    }
}
