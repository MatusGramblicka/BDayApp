using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models
{
    public class Event
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is a required field.")]
        [MaxLength(30, ErrorMessage = "Maximum length for the Name is 30 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Date is a required field.")]
        public DateTime Date { get; set; }

        public string ImageUrl { get; set; }
        
        [ForeignKey(nameof(User))]
        public string UserId { get; set; }

        public User User { get; set; }
    }
}
