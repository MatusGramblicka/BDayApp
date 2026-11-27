using BDayClient.Features;
using BDayClient.Http;
using Contracts.DataTransferObjects.Person;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

/// <summary>The persons whose birthday or nameday is within the next <see cref="WithinDays"/> days, soonest first.</summary>
public partial class PersonTableAdmin
{
    public const int WithinDays = 30;

    private IReadOnlyList<UpcomingPerson> _upcoming = [];

    /// <summary>All persons; null while they are loading.</summary>
    [Parameter]
    public IReadOnlyList<PersonDto>? Persons { get; set; }

    /// <summary>The day to count from; today when not set.</summary>
    [Parameter]
    public DateOnly? Today { get; set; }

    [Inject]
    private ApiUrls ApiUrls { get; set; } = default!;

    protected override void OnParametersSet()
    {
        _upcoming = Persons is null
            ? []
            : UpcomingPerson.Within(Persons, Today ?? DateOnly.FromDateTime(DateTime.Today), WithinDays);
    }
}
