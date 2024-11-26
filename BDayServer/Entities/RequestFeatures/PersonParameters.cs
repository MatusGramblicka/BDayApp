namespace Entities.RequestFeatures;

public class PersonParameters : RequestParameters
{
    public PersonParameters()
    {
        OrderBy = "dayOfBirth ascending";
    }

    public uint MinAge { get; set; }
    public uint MaxAge { get; set; } = int.MaxValue;

    public bool ValidAgeRange => MaxAge > MinAge;

    public string? SearchTerm { get; set; }
}