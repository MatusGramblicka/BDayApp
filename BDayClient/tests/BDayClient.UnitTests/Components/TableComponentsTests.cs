using BDayClient.Components;
using BDayClient.Http;
using Contracts.DataTransferObjects.Person;
using Contracts.DataTransferObjects.User;
using Microsoft.Extensions.DependencyInjection;

namespace BDayClient.UnitTests.Components;

public class TableComponentsTests : BunitContext
{
    public TableComponentsTests()
    {
        Services.AddSingleton(new ApiUrls("https://api.test"));
        AddAuthorization().SetAuthorized("admin@test.com").SetRoles("Administrator");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static PersonDto Person(string name, DateOnly nameDay, DateOnly? birthday = null, string? imageUrl = null) =>
        new() { Id = Guid.NewGuid(), Name = name, DayOfNameDay = nameDay, DayOfBirth = birthday, ImageUrl = imageUrl };

    [Fact]
    public void PersonTable_WhileLoading_SaysLoading()
    {
        var cut = Render<PersonTable>(ps => ps.Add(p => p.Persons, null));

        cut.Markup.Should().Contain("Loading persons...");
        cut.FindAll("table").Should().BeEmpty();
    }

    // Review finding: an empty list looked like "Loading persons..." forever.
    [Fact]
    public void PersonTable_NoPersons_SaysSo()
    {
        var cut = Render<PersonTable>(ps => ps.Add(p => p.Persons, []));

        cut.Markup.Should().Contain("No persons found.").And.NotContain("Loading");
    }

    [Fact]
    public void PersonTable_RendersRelativeAndAbsoluteImageUrls_WithAltText()
    {
        var cut = Render<PersonTable>(ps => ps.Add(p => p.Persons, new List<PersonDto>
        {
            Person("Anna", new DateOnly(2000, 7, 26), imageUrl: "StaticFiles/Images/anna.png"),
            Person("Boris", new DateOnly(2000, 5, 2), imageUrl: "https://old.azurewebsites.net/StaticFiles/Images/boris.png"),
            Person("Cyril", new DateOnly(2000, 7, 5))
        }));

        var images = cut.FindAll("img");
        images.Select(i => i.GetAttribute("src")).Should().Equal(
            "https://api.test/StaticFiles/Images/anna.png",
            "https://old.azurewebsites.net/StaticFiles/Images/boris.png");
        images.Select(i => i.GetAttribute("alt")).Should().Equal("Photo of Anna", "Photo of Boris");
    }

    [Fact]
    public void PersonTableAdmin_ShowsOnlyUpcoming_SortedByDaysUntil()
    {
        var today = new DateOnly(2026, 12, 20);
        var cut = Render<PersonTableAdmin>(ps => ps
            .Add(p => p.Today, today)
            .Add(p => p.Persons, new List<PersonDto>
            {
                Person("Late", new DateOnly(2000, 1, 15)),
                Person("Far", new DateOnly(2000, 6, 1)),
                Person("Today", new DateOnly(2000, 12, 20)),
                Person("NewYear", new DateOnly(2000, 1, 5), birthday: new DateOnly(1990, 3, 3))
            }));

        var rows = cut.FindAll("tbody tr");
        rows.Select(r => r.QuerySelectorAll("td")[1].TextContent.Trim()).Should().Equal("Today", "NewYear", "Late");
        rows.Select(r => r.QuerySelectorAll("td")[5].TextContent.Trim())
            .Should().Equal("Nameday today", "Nameday in 16 days", "Nameday in 26 days");
    }

    [Fact]
    public void PersonTableAdmin_LoadingAndNothingUpcoming_AreDifferent()
    {
        Render<PersonTableAdmin>(ps => ps.Add(p => p.Persons, null)).Markup.Should().Contain("Loading persons...");

        Render<PersonTableAdmin>(ps => ps
                .Add(p => p.Today, new DateOnly(2026, 12, 20))
                .Add(p => p.Persons, new List<PersonDto> { Person("Far", new DateOnly(2000, 6, 1)) }))
            .Markup.Should().Contain("No birthdays or namedays in the next 30 days.");
    }

    [Fact]
    public void UsersTable_RendersUsersWithTwoFactorSwitch()
    {
        var cut = Render<UsersTable>(ps => ps
            .Add(p => p.Users, new List<UserLite> { new() { Email = "a@test.com", TwoFactorEnabled = true } })
            .Add(p => p.LoggedUser, "b@test.com"));

        cut.FindAll(".rz-switch").Should().ContainSingle();
        cut.Markup.Should().Contain("Update to Administrator").And.NotContain("Admininstrator");
    }

    [Fact]
    public void UsersTable_LoggedUser_CannotChangeOrDeleteThemselves()
    {
        var cut = Render<UsersTable>(ps => ps
            .Add(p => p.Users, new List<UserLite> { new() { Email = "me@test.com", IsAdmin = true } })
            .Add(p => p.LoggedUser, "ME@test.com"));

        cut.FindAll("tbody button").Should().OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void UsersTable_LoadingAndEmpty_AreDifferent()
    {
        Render<UsersTable>(ps => ps.Add(p => p.Users, null)).Markup.Should().Contain("Loading users...");
        Render<UsersTable>(ps => ps.Add(p => p.Users, [])).Markup.Should().Contain("No users found.");
    }
}
