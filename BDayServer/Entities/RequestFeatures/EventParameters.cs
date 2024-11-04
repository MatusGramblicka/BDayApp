namespace Entities.RequestFeatures;

public class EventParameters : RequestParameters
{
    public EventParameters()
    {
        OrderBy = "date ascending";
    }
    public uint MinAge { get; set; }
    public uint MaxAge { get; set; } = int.MaxValue;

    public bool ValidAgeRange => MaxAge > MinAge;

    public string SearchTerm { get; set; }
}