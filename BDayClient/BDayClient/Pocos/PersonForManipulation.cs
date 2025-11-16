using System.ComponentModel.DataAnnotations;

namespace BDayClient.Pocos;

public class PersonForManipulation
{
    [Required(ErrorMessage = "Name is a required field.")]
    [MaxLength(10, ErrorMessage = "Maximum length for the Name is 10 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Surname is a required field.")]
    [MaxLength(30, ErrorMessage = "Maximum length for the Surname is 30 characters.")]
    public string Surname { get; set; } = string.Empty;
    
    public DateOnly? DayOfBirth { get; set; }

    public DateOnly DayOfNameDay { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
}