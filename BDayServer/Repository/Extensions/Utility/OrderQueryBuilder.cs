using System.Reflection;

namespace Repository.Extensions.Utility;

public static class OrderQueryBuilder
{
    /// <summary>
    /// Turns "name desc, surname" into "Name descending, Surname ascending" (a Dynamic LINQ order clause).
    /// Unknown properties are skipped; "desc"/"descending" in any case means descending, anything else ascending.
    /// </summary>
    public static string CreateOrderQuery<T>(string? orderByQueryString)
    {
        if (string.IsNullOrWhiteSpace(orderByQueryString))
            return string.Empty;

        var propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var clauses = new List<string>();

        foreach (var param in orderByQueryString.Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = param.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var objectProperty = propertyInfos.FirstOrDefault(pi =>
                pi.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));

            if (objectProperty is null)
                continue;

            var descending = tokens.Length > 1 &&
                             (tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase) ||
                              tokens[1].Equals("descending", StringComparison.OrdinalIgnoreCase));

            clauses.Add($"{objectProperty.Name} {(descending ? "descending" : "ascending")}");
        }

        return string.Join(", ", clauses);
    }
}
