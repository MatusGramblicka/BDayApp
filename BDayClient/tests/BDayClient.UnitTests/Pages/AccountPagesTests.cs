using System.Net;
using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pages;
using BDayClient.Pocos;
using BDayClient.Shared;
using Blazored.Toast.Configuration;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace BDayClient.UnitTests.Pages;

public class AccountPagesTests : BunitContext
{
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly Mock<IUsersHttpRepository> _usersRepository = new();
    private readonly Mock<IToastService> _toasts = new();

    public AccountPagesTests()
    {
        Services.AddSingleton(_authService.Object);
        Services.AddSingleton(_usersRepository.Object);
        Services.AddSingleton(_toasts.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private static ApiException Handled(HttpStatusCode status) =>
        new(new ApiError(status, [ApiErrorReader.TooManyRequestsMessage], Handled: true));

    private IRenderedComponent<Login> RenderLogin(string uri = "/login")
    {
        Navigation.NavigateTo(uri);
        var cut = Render<Login>();
        cut.Find("#email").Change("john@test.com");
        cut.Find("#password").Change("secret");
        return cut;
    }

    // Review finding: after navigating to the 2FA page the login carried on and flashed an empty red alert.
    [Fact]
    public void Login_TwoStepVerificationRequired_GoesThereWithoutAnError()
    {
        _authService.Setup(s => s.Login(It.IsAny<UserForAuthentication>()))
            .ReturnsAsync(new AuthResponseDto { Is2StepVerificationRequired = true, Provider = "Email" });
        var cut = RenderLogin("/login?returnUrl=%2Fusers");

        cut.Find("form").Submit();

        var uri = new Uri(Navigation.Uri);
        uri.AbsolutePath.Should().Be("/twostepverification");
        var query = QueryHelpers.ParseQuery(uri.Query);
        query["provider"].ToString().Should().Be("Email");
        query["email"].ToString().Should().Be("john@test.com");
        query["returnUrl"].ToString().Should().Be("/users");
        cut.FindAll(".alert-danger").Should().BeEmpty();
    }

    [Fact]
    public void Login_Success_ReturnsToThePageThatAskedForIt()
    {
        _authService.Setup(s => s.Login(It.IsAny<UserForAuthentication>()))
            .ReturnsAsync(new AuthResponseDto { IsAuthSuccessful = true });
        var cut = RenderLogin("/login?returnUrl=%2FupdatePerson%2F5");

        cut.Find("form").Submit();

        Navigation.Uri.Should().Be("http://localhost/updatePerson/5");
    }

    [Fact]
    public void Login_Success_IgnoresAReturnUrlToAnotherSite()
    {
        _authService.Setup(s => s.Login(It.IsAny<UserForAuthentication>()))
            .ReturnsAsync(new AuthResponseDto { IsAuthSuccessful = true });
        var cut = RenderLogin("/login?returnUrl=https%3A%2F%2Fevil.test%2F");

        cut.Find("form").Submit();

        Navigation.Uri.Should().Be("http://localhost/");
    }

    [Fact]
    public void Login_Refused_ShowsTheServersMessage()
    {
        _authService.Setup(s => s.Login(It.IsAny<UserForAuthentication>()))
            .ReturnsAsync(new AuthResponseDto { ErrorMessage = "Invalid credentials" });
        var cut = RenderLogin();

        cut.Find("form").Submit();

        cut.Find(".alert-danger").TextContent.Trim().Should().Be("Invalid credentials");
    }

    [Fact]
    public void Login_FailureAlreadyShownAsToast_ShowsNoSecondMessage()
    {
        _authService.Setup(s => s.Login(It.IsAny<UserForAuthentication>())).ThrowsAsync(Handled(HttpStatusCode.TooManyRequests));
        var cut = RenderLogin();

        cut.Find("form").Submit();

        cut.FindAll(".alert-danger").Should().BeEmpty();
        Navigation.Uri.Should().EndWith("/login");
    }

    [Fact]
    public void TwoStepVerification_Success_ReturnsToThePageThatAskedForIt()
    {
        _authService.Setup(s => s.LoginVerification(It.Is<TwoFactorVerificationDto>(d =>
                d.Email == "john@test.com" && d.TwoFactorToken == "123456")))
            .ReturnsAsync(new AuthResponseDto { IsAuthSuccessful = true });
        Navigation.NavigateTo("/twostepverification?provider=Email&email=john%40test.com&returnUrl=%2Fusers");
        var cut = Render<TwoStepVerification>();

        cut.Find("#token").Change("123456");
        cut.Find("form").Submit();

        Navigation.Uri.Should().Be("http://localhost/users");
    }

    // Review finding: the server answers 204, so every successful request showed "Forgot password failed".
    [Fact]
    public void ForgotPassword_Success_SaysTheLinkWasSent()
    {
        _authService.Setup(s => s.ForgotPassword(It.IsAny<BDayClient.Pocos.ForgotPassword>())).ReturnsAsync(ApiResult.Success());
        var cut = Render<BDayClient.Pages.ForgotPassword>();

        cut.Find("#email").Change("john@test.com");
        cut.Find("form").Submit();

        cut.Find(".alert-success").TextContent.Should().Contain("The link has been sent");
        cut.FindAll(".alert-danger").Should().BeEmpty();
    }

    [Fact]
    public void ForgotPassword_Refused_ShowsTheServersMessages()
    {
        _authService.Setup(s => s.ForgotPassword(It.IsAny<BDayClient.Pocos.ForgotPassword>()))
            .ReturnsAsync(ApiResult.Failure(["The Email field is not a valid e-mail address."]));
        var cut = Render<BDayClient.Pages.ForgotPassword>();

        cut.Find("#email").Change("john@test.com");
        cut.Find("form").Submit();

        cut.Find(".alert-danger").TextContent.Should().Contain("The Email field is not a valid e-mail address.");
    }

    [Fact]
    public void EmailConfirmation_Success_SaysConfirmed()
    {
        _authService.Setup(s => s.EmailConfirmation("john@test.com", "a+b")).ReturnsAsync(ApiResult.Success());
        Navigation.NavigateTo("/emailconfirmation?email=john%40test.com&token=a%2Bb");

        var cut = Render<EmailConfirmation>();

        cut.Find(".alert-success").TextContent.Should().Contain("Your email has been confirmed");
    }

    [Fact]
    public void EmailConfirmation_Refused_ShowsFailureWithTheServersMessage()
    {
        _authService.Setup(s => s.EmailConfirmation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(ApiResult.Failure(["Invalid token."]));
        Navigation.NavigateTo("/emailconfirmation?email=john%40test.com&token=x");

        var cut = Render<EmailConfirmation>();

        cut.Find(".alert-danger").TextContent.Should().Contain("Email confirmation failed.").And.Contain("Invalid token.");
    }

    // Review finding: reset failures showed nothing or an empty red box.
    [Fact]
    public void ResetPassword_Refused_ShowsTheServersMessages()
    {
        _authService.Setup(s => s.ResetPassword(It.IsAny<BDayClient.Pocos.ResetPassword>()))
            .ReturnsAsync(new ResetPasswordResponseDto { Errors = ["Invalid token."] });
        Navigation.NavigateTo("/resetpassword?email=john%40test.com&token=x");
        var cut = Render<BDayClient.Pages.ResetPassword>();

        cut.Find("#password").Change("Secret1!");
        cut.Find("#confirm").Change("Secret1!");
        cut.Find("form").Submit();

        cut.Find(".alert-danger").TextContent.Trim().Should().Be("Invalid token.");
    }

    [Fact]
    public void Registration_Success_AsksToConfirmTheEmailAddress()
    {
        _authService.Setup(s => s.RegisterUser(It.IsAny<UserForRegistration>()))
            .ReturnsAsync(new ResponseDto { IsSuccessfulRegistration = true });
        var cut = Render<Registration>();

        cut.Find("#email").Change("new@test.com");
        cut.Find("#password").Change("Secret1!");
        cut.Find("#confirm").Change("Secret1!");
        cut.Find("form").Submit();

        cut.Find(".alert-success").TextContent.Should().Contain("check your email");
        cut.FindAll("form").Should().BeEmpty();
    }

    [Fact]
    public void Registration_Refused_ShowsTheServersMessages()
    {
        _authService.Setup(s => s.RegisterUser(It.IsAny<UserForRegistration>()))
            .ReturnsAsync(new ResponseDto { Errors = ["Email 'new@test.com' is already taken."] });
        var cut = Render<Registration>();

        cut.Find("#email").Change("new@test.com");
        cut.Find("#password").Change("Secret1!");
        cut.Find("#confirm").Change("Secret1!");
        cut.Find("form").Submit();

        cut.Find(".alert-danger").TextContent.Should().Contain("already taken");
    }

    [Fact]
    public void RedirectToLogin_GoesToLoginAndBack()
    {
        Navigation.NavigateTo("/persons");

        Render<RedirectToLogin>();

        Navigation.Uri.Should().Be("http://localhost/login?returnUrl=%2Fpersons");
    }

    [Fact]
    public void Users_ShowsTheLoggedInUser_AndReportsRefusedChanges()
    {
        AddAuthorization().SetAuthorized("me@test.com").SetRoles("Administrator");
        var other = new UserLite { Email = "other@test.com" };
        _usersRepository.Setup(r => r.GetUsers()).ReturnsAsync([new UserLite { Email = "me@test.com", IsAdmin = true }, other]);
        _usersRepository.Setup(r => r.UpdateUser(It.IsAny<UserLite>()))
            .ThrowsAsync(new ApiException(new ApiError(HttpStatusCode.BadRequest, ["The last administrator cannot be changed."], false)));
        var cut = Render<Users>();

        var rows = cut.FindAll("tbody tr");
        rows[0].QuerySelectorAll("button").Should().OnlyContain(b => b.HasAttribute("disabled"), "nobody changes their own account");
        cut.Find("[aria-label='Administrator role for other@test.com']").Click();

        _toasts.Verify(t => t.ShowError("The last administrator cannot be changed.", It.IsAny<Action<ToastSettings>?>()), Times.Once);
        _toasts.Verify(t => t.ShowSuccess(It.IsAny<string>(), It.IsAny<Action<ToastSettings>?>()), Times.Never);
        _usersRepository.Verify(r => r.GetUsers(), Times.Exactly(2), "the list is reloaded to show the server's state");
    }

    [Fact]
    public void Users_SuccessfulChange_IsConfirmed()
    {
        AddAuthorization().SetAuthorized("me@test.com").SetRoles("Administrator");
        _usersRepository.Setup(r => r.GetUsers()).ReturnsAsync([new UserLite { Email = "other@test.com" }]);
        _usersRepository.Setup(r => r.UpdateUser(It.IsAny<UserLite>())).Returns(Task.CompletedTask);
        var cut = Render<Users>();

        cut.Find("[aria-label='Administrator role for other@test.com']").Click();

        _toasts.Verify(t => t.ShowSuccess("other@test.com is now an administrator.", It.IsAny<Action<ToastSettings>?>()), Times.Once);
    }
}
