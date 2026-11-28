using System.ComponentModel.DataAnnotations;

namespace BDayClient.Pocos;

public class EventForManipulation
{
    [Required(ErrorMessage = "Name is a required field.")]
    [MaxLength(30, ErrorMessage = "Maximum length for the Name is 30 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Day and month, like a person's birthday and name day: edited with the day/month picker (which stores the year
    /// <see cref="Features.DayMonth.StorageYear"/>), shown without the year, sent to the API with time 00:00.
    /// </summary>
    public DateOnly Date { get; set; }

    public string? ImageUrl { get; set; }

    // The API takes a DateTime; an event is a whole day, so the time is always midnight.
    protected DateTime DateAtMidnight => Date.ToDateTime(TimeOnly.MinValue);

    // An optional input left empty is sent as null, not as "".
    protected static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
