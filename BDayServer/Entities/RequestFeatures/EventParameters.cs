namespace Entities.RequestFeatures;

public class EventParameters : RequestParameters
{
    public EventParameters()
    {
        OrderBy = "date ascending";
    }

    public string? SearchTerm { get; set; }
}
