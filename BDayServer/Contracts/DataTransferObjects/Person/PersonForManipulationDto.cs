using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.Person;

public record PersonForManipulationDto
{
    [Required(ErrorMessage = "Name is a required field.")]
    [MaxLength(20, ErrorMessage = "Maximum length for the Name is 20 characters.")]
    public string Name { get; init; } = string.Empty;

    [MaxLength(30, ErrorMessage = "Maximum length for the Surname is 30 characters.")]
    public string? Surname { get; init; }
        
    public DateOnly? DayOfBirth { get; init; }

    public DateOnly DayOfNameDay { get; init; }

    public string? ImageUrl { get; init; }
}