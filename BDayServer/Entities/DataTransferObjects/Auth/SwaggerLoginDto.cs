namespace Entities.DataTransferObjects.Auth;

public class SwaggerLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}