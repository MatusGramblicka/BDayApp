using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.Event;

public class EventForManipulationDto 
{       
    [Required(ErrorMessage = "Name is a required field.")]
    [MaxLength(30, ErrorMessage = "Maximum length for the Name is 30 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Date is a required field.")]
    public DateTime Date { get; set; }

    public string ImageUrl { get; set; }        
}