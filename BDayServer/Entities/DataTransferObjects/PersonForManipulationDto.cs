using System;
using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects
{
    public class PersonForManipulationDto
    {
        [Required(ErrorMessage = "Name is a required field.")]
        [MaxLength(10, ErrorMessage = "Maximum length for the Name is 10 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Surname is a required field.")]
        [MaxLength(30, ErrorMessage = "Maximum length for the Surname is 30 characters.")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "DayOfBirth is a required field.")]
        public DateTime DayOfBirth { get; set; }

        public DateTime DayOfNameDay { get; set; }

        public string ImageUrl { get; set; }
    }
}
