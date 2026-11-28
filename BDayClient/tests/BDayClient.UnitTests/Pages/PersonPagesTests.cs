using System.Net;
using BDayClient.Features;
using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pages;
using BDayClient.Shared;
using Blazored.Toast.Configuration;
using Blazored.Toast.Services;
using Bunit.TestDoubles;
using Contracts.DataTransferObjects.Person;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BDayClient.UnitTests.Pages;

/// <summary>Regression tests for the person pages (several were reproduced by the review against the old code).</summary>
public class PersonPagesTests : BunitContext
{
    private readonly Mock<IPersonHttpRepository> _repository = new();
    private readonly Mock<IToastService> _toasts = new();
    private readonly BunitAuthorizationContext _authorization;

    public PersonPagesTests()
    {
        Services.AddSingleton(_repository.Object);
        Services.AddSingleton(_toasts.Object);
        Services.AddSingleton(new ApiUrls("https://api.test"));
        _authorization = AddAuthorization();
        _authorization.SetAuthorized("john@test.com");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly Guid AnnaId = Guid.NewGuid();

    private static PersonDto Anna => new()
    {
        Id = AnnaId, Name = "Anna", Surname = "Novak", DayOfNameDay = new DateOnly(2000, 7, 26),
        DayOfBirth = new DateOnly(1990, 5, 1), ImageUrl = "StaticFiles/Images/anna.png"
    };

    private static ApiException ServerError(string message, bool handled = false) =>
        new(new ApiError(HttpStatusCode.BadRequest, [message], handled));

    private string CurrentUri => Services.GetRequiredService<NavigationManager>().Uri;

    private void VerifyNoSuccessToast() =>
        _toasts.Verify(t => t.ShowSuccess(It.IsAny<string>(), It.IsAny<Action<ToastSettings>?>()), Times.Never);

    private void VerifyErrorToast(string message) =>
        _toasts.Verify(t => t.ShowError(message, It.IsAny<Action<ToastSettings>?>()), Times.Once);

    private void VerifyNoErrorToast() =>
        _toasts.Verify(t => t.ShowError(It.IsAny<string>(), It.IsAny<Action<ToastSettings>?>()), Times.Never);

    // Review finding: the form rendered with a null EditContext while the person loaded and threw every time.
    [Fact]
    public void UpdatePerson_WhileLoading_ShowsLoadingInsteadOfThrowing()
    {
        var person = new TaskCompletionSource<PersonDto?>();
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).Returns(person.Task);

        var cut = Render<UpdatePerson>(ps => ps.Add(p => p.Id, AnnaId));

        cut.Markup.Should().Contain("Loading person...");
        cut.FindAll("form").Should().BeEmpty();

        person.SetResult(Anna);
        cut.WaitForAssertion(() => cut.Find("#name").GetAttribute("value").Should().Be("Anna"));
        cut.Find("#surname").GetAttribute("value").Should().Be("Novak");
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue("nothing was changed yet");
    }

    [Fact]
    public void UpdatePerson_UnknownPerson_GoesTo404()
    {
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).ReturnsAsync((PersonDto?)null);

        Render<UpdatePerson>(ps => ps.Add(p => p.Id, AnnaId));

        CurrentUri.Should().EndWith("/404");
    }

    [Fact]
    public void UpdatePerson_LoadFails_ShowsTheServerMessage()
    {
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).ThrowsAsync(ServerError("Boom"));

        var cut = Render<UpdatePerson>(ps => ps.Add(p => p.Id, AnnaId));

        cut.Markup.Should().Contain("The person could not be loaded.");
        VerifyErrorToast("Boom");
    }

    // Review finding: a success toast was shown whatever the server answered.
    [Fact]
    public void UpdatePerson_SaveRefused_ShowsServerMessage_AndNoSuccessToast()
    {
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).ReturnsAsync(Anna);
        _repository.Setup(r => r.UpdatePerson(AnnaId, It.IsAny<PersonForUpdateDto>()))
            .ThrowsAsync(ServerError("Person does not exist"));
        var cut = Render<UpdatePerson>(ps => ps.Add(p => p.Id, AnnaId));

        cut.Find("#name").Change("Anička");
        cut.Find("form").Submit();

        VerifyErrorToast("Person does not exist");
        VerifyNoSuccessToast();
        cut.Find("#name").GetAttribute("value").Should().Be("Anička", "the input is kept for another try");
    }

    [Fact]
    public void UpdatePerson_Saved_SendsTheChanges_AndIsUntouchedAgain()
    {
        PersonForUpdateDto? sent = null;
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).ReturnsAsync(Anna);
        _repository.Setup(r => r.UpdatePerson(AnnaId, It.IsAny<PersonForUpdateDto>()))
            .Callback<Guid, PersonForUpdateDto>((_, dto) => sent = dto)
            .Returns(Task.CompletedTask);
        var cut = Render<UpdatePerson>(ps => ps.Add(p => p.Id, AnnaId));

        cut.Find("#name").Change("Anička");
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeFalse();
        cut.Find("form").Submit();

        sent.Should().BeEquivalentTo(new PersonForUpdateDto
        {
            Name = "Anička", Surname = "Novak", DayOfBirth = new DateOnly(1990, 5, 1),
            DayOfNameDay = new DateOnly(2000, 7, 26), ImageUrl = "StaticFiles/Images/anna.png"
        });
        _toasts.Verify(t => t.ShowSuccess("Person \"Anička\" was updated.", It.IsAny<Action<ToastSettings>?>()), Times.Once);
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue();
        cut.Find("#name").GetAttribute("value").Should().Be("Anička");
    }

    [Fact]
    public void UpdatePerson_ClearedSurname_IsSentAsNull()
    {
        PersonForUpdateDto? sent = null;
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).ReturnsAsync(Anna);
        _repository.Setup(r => r.UpdatePerson(AnnaId, It.IsAny<PersonForUpdateDto>()))
            .Callback<Guid, PersonForUpdateDto>((_, dto) => sent = dto)
            .Returns(Task.CompletedTask);
        var cut = Render<UpdatePerson>(ps => ps.Add(p => p.Id, AnnaId));

        cut.Find("#surname").Change("");
        cut.Find("form").Submit();

        sent!.Surname.Should().BeNull();
    }

    // Review finding (verified): after a successful create the reset form showed validation errors, and the first
    // create sent the nameday in the current year instead of the pickers' year 2000.
    [Fact]
    public void CreatePerson_AfterSuccessfulCreate_ResetsToAnUntouchedForm()
    {
        var sent = new List<PersonForCreationDto>();
        _repository.Setup(r => r.CreatePerson(It.IsAny<PersonForCreationDto>()))
            .Callback<PersonForCreationDto>(sent.Add).Returns(Task.CompletedTask);
        var cut = Render<CreatePerson>();
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue();

        cut.Find("#name").Change("Anna");
        cut.Find("form").Submit();

        sent.Should().ContainSingle();
        sent[0].Name.Should().Be("Anna");
        sent[0].Surname.Should().BeNull();
        sent[0].ImageUrl.Should().BeNull();
        sent[0].DayOfNameDay.Should().Be(DayMonth.InStorageYear(DateOnly.FromDateTime(DateTime.Today)));
        _toasts.Verify(t => t.ShowSuccess("Person \"Anna\" was added.", It.IsAny<Action<ToastSettings>?>()), Times.Once);
        cut.Find("#name").GetAttribute("value").Should().BeNullOrEmpty();
        // Since .NET 11 every ValidationMessage renders a hidden placeholder; only visible ones are messages.
        cut.FindAll(".validation-message:not([hidden])").Select(m => m.TextContent).Should().BeEmpty();
        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void CreatePerson_SubmittedWithoutName_ShowsTheValidationMessage_AndSendsNothing()
    {
        var cut = Render<CreatePerson>();

        cut.Find("form").Submit();

        cut.FindAll(".validation-message:not([hidden])").Select(m => m.TextContent.Trim())
            .Should().Equal("Name is a required field.");
        _repository.Verify(r => r.CreatePerson(It.IsAny<PersonForCreationDto>()), Times.Never);
    }

    [Fact]
    public void CreatePerson_Refused_ShowsServerMessage_KeepsTheInput_AndNoSuccessToast()
    {
        _repository.Setup(r => r.CreatePerson(It.IsAny<PersonForCreationDto>()))
            .ThrowsAsync(ServerError("Maximum length for the Name is 20 characters."));
        var cut = Render<CreatePerson>();

        cut.Find("#name").Change("Anna");
        cut.Find("form").Submit();

        VerifyErrorToast("Maximum length for the Name is 20 characters.");
        VerifyNoSuccessToast();
        cut.Find("#name").GetAttribute("value").Should().Be("Anna");
    }

    [Fact]
    public void CreatePerson_FailureAlreadyShownCentrally_ShowsNoSecondToast()
    {
        _repository.Setup(r => r.CreatePerson(It.IsAny<PersonForCreationDto>()))
            .ThrowsAsync(ServerError("Too many requests - please try again in a minute", handled: true));
        var cut = Render<CreatePerson>();

        cut.Find("#name").Change("Anna");
        cut.Find("form").Submit();

        VerifyNoErrorToast();
        VerifyNoSuccessToast();
    }

    // Review finding: an image change enabled the submit button even when the rest of the form was invalid.
    [Fact]
    public async Task CreatePerson_NewImage_DoesNotEnableSubmitWhileTheFormIsInvalid()
    {
        _authorization.SetRoles("Administrator");
        var cut = Render<CreatePerson>();

        var upload = cut.FindComponent<ImageUpload>();
        await cut.InvokeAsync(() => upload.Instance.OnChange.InvokeAsync("StaticFiles/Images/new.png"));

        cut.Find("button[type=submit]").HasAttribute("disabled").Should().BeTrue("the name is still missing");
        cut.Find("img.image-preview").GetAttribute("src").Should().Be("https://api.test/StaticFiles/Images/new.png");
    }

    [Fact]
    public void PersonDetails_WhileLoading_ShowsNoPlaceholderCard()
    {
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<PersonDto?>().Task);

        var cut = Render<PersonDetails>(ps => ps.Add(p => p.PersonId, AnnaId));

        cut.FindAll(".card").Should().BeEmpty();
        cut.Markup.Should().Contain("Loading person...");
    }

    [Fact]
    public void PersonDetails_Loaded_ShowsThePerson()
    {
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).ReturnsAsync(Anna);

        var cut = Render<PersonDetails>(ps => ps.Add(p => p.PersonId, AnnaId));

        cut.Find(".person-title").TextContent.Should().Contain("Anna").And.Contain("Novak");
        cut.Markup.Should().Contain("Birthday: 01.05.").And.Contain("Nameday: 26.07.");
    }

    [Fact]
    public void PersonDetails_UnknownPerson_GoesTo404()
    {
        _repository.Setup(r => r.GetPerson(AnnaId, It.IsAny<CancellationToken>())).ReturnsAsync((PersonDto?)null);

        Render<PersonDetails>(ps => ps.Add(p => p.PersonId, AnnaId));

        CurrentUri.Should().EndWith("/404");
    }

    // Review finding: a user without persons (or a search without hits) saw "Loading persons..." forever.
    [Fact]
    public void Persons_NoPersons_SaysSoInsteadOfLoading()
    {
        _repository.Setup(r => r.GetPersons(It.IsAny<PersonParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagingResponse<PersonDto>());

        var cut = Render<BDayClient.Pages.Persons>();

        cut.Markup.Should().Contain("No persons found.").And.NotContain("Loading persons...");
    }

    [Fact]
    public void Persons_Delete_ConfirmsAndReportsSuccess()
    {
        _repository.Setup(r => r.GetPersons(It.IsAny<PersonParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagingResponse<PersonDto> { Items = [Anna], MetaData = new MetaData { CurrentPage = 1, TotalPages = 1 } });
        _repository.Setup(r => r.DeletePerson(AnnaId)).Returns(Task.CompletedTask);
        var cut = Render<BDayClient.Pages.Persons>();

        cut.Find("button.btn-danger").Click();      // Delete in the table
        cut.Find(".modal button.btn-success").Click(); // OK in the confirmation

        _repository.Verify(r => r.DeletePerson(AnnaId), Times.Once);
        _toasts.Verify(t => t.ShowSuccess("Person \"Anna\" was deleted.", It.IsAny<Action<ToastSettings>?>()), Times.Once);
    }

    [Fact]
    public void Persons_DeleteRefused_ShowsServerMessage()
    {
        _repository.Setup(r => r.GetPersons(It.IsAny<PersonParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagingResponse<PersonDto> { Items = [Anna] });
        _repository.Setup(r => r.DeletePerson(AnnaId)).ThrowsAsync(ServerError("Person does not exist"));
        var cut = Render<BDayClient.Pages.Persons>();

        cut.Find("button.btn-danger").Click();
        cut.Find(".modal button.btn-success").Click();

        VerifyErrorToast("Person does not exist");
        VerifyNoSuccessToast();
    }

    [Fact]
    public void Index_UsesAllPersons()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _repository.Setup(r => r.GetAllPersons(It.IsAny<CancellationToken>())).ReturnsAsync(
            [new PersonDto { Id = Guid.NewGuid(), Name = "Celebrating", DayOfNameDay = DayMonth.InStorageYear(today) }]);

        var cut = Render<BDayClient.Pages.Index>();

        cut.Markup.Should().Contain("Celebrating").And.Contain("Nameday today");
        _repository.Verify(r => r.GetPersons(It.IsAny<PersonParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ImageUpload_UploadsUnderAPngName()
    {
        using var content = ImageUpload.CreateUploadContent(new MemoryStream([1, 2, 3]), "holiday photo.jfif");

        var part = content.Single();
        part.Headers.ContentDisposition!.Name!.Trim('"').Should().Be("image");
        part.Headers.ContentDisposition.FileName!.Trim('"').Should().Be("holiday photo.png");
        part.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Fact]
    public void ImageUpload_AcceptsOnlyImages()
    {
        var cut = Render<ImageUpload>();

        cut.Find("input[type=file]").GetAttribute("accept").Should().Be("image/*");
        cut.FindAll("img").Should().BeEmpty("there is no image yet");
    }
}
