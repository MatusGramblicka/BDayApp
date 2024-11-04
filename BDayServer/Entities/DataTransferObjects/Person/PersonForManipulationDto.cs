using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects.Person;

public record PersonForManipulationDto
{
    [Required(ErrorMessage = "Name is a required field.")]
    [MaxLength(10, ErrorMessage = "Maximum length for the Name is 10 characters.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Surname is a required field.")]
    [MaxLength(30, ErrorMessage = "Maximum length for the Surname is 30 characters.")]
    public string Surname { get; init; } = string.Empty;

    [Required(ErrorMessage = "DayOfBirth is a required field.")]
    public DateTime DayOfBirth { get; init; }

    public DateTime DayOfNameDay { get; init; }

    public string ImageUrl { get; init; } = string.Empty;
}