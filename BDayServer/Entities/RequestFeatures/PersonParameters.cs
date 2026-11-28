namespace Entities.RequestFeatures;

public class PersonParameters : RequestParameters
{
    public PersonParameters()
    {
        OrderBy = "dayOfBirth ascending";
    }

    public string? SearchTerm { get; set; }
}
