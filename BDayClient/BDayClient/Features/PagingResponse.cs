using Entities.RequestFeatures;

namespace BDayClient.Features;

public class PagingResponse<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public MetaData MetaData { get; set; }
}