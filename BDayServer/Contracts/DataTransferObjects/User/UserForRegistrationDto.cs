using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.User;

public record UserForRegistrationDto
{
    // The e-mail address is also the user name; both columns hold at most 50 characters.
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not a valid e-mail address.")]
    [MaxLength(50, ErrorMessage = "Maximum length for the Email is 50 characters.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(100, ErrorMessage = "Maximum length for the Password is 100 characters.")]
    public string Password { get; init; } = string.Empty;

    [Compare(nameof(Password),
        ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>Page that the confirmation link in the e-mail opens; must belong to this application.</summary>
    [Required(ErrorMessage = "ClientUri is required.")]
    public string ClientUri { get; init; } = string.Empty;
}
