using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models;

public class Person
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is a required field.")]
    [MaxLength(10, ErrorMessage = "Maximum length for the Name is 10 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Surname is a required field.")]
    [MaxLength(30, ErrorMessage = "Maximum length for the Surname is 30 characters.")]
    public string Surname { get; set; }
    
    public DateOnly? DayOfBirth { get; set; }

    [Required(ErrorMessage = "Name day is a required field.")]
    public DateOnly DayOfNameDay { get; set; }

    public string? ImageUrl { get; set; }

    [ForeignKey(nameof(User))]
    public string UserId { get; set; }

    public User User { get; set; }
}