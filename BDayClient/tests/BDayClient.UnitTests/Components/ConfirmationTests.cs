using BDayClient.Shared;
using Microsoft.AspNetCore.Components.Web;

namespace BDayClient.UnitTests.Components;

public class ConfirmationTests : BunitContext
{
    private int _okClicks;

    private IRenderedComponent<Confirmation> RenderConfirmation() =>
        Render<Confirmation>(ps => ps
            .Add(p => p.BodyMessage, "Delete Anna?")
            .Add(p => p.OnOKClicked, () => _okClicks++));

    [Fact]
    public void Renders_BodyMessage_AndIsHiddenInitially()
    {
        var cut = RenderConfirmation();

        cut.Find(".modal-body p").TextContent.Should().Be("Delete Anna?");
        cut.FindAll(".modal-backdrop").Should().BeEmpty();
        cut.Find(".modal").GetAttribute("style").Should().Contain("display:none");
    }

    [Fact]
    public void IsAnAccessibleModalDialog()
    {
        var cut = RenderConfirmation();

        var dialog = cut.Find(".modal");
        dialog.GetAttribute("aria-modal").Should().Be("true");
        cut.Find("#" + dialog.GetAttribute("aria-labelledby")).TextContent.Should().Be("Confirmation");
        cut.FindAll("[data-dismiss]").Should().BeEmpty();
    }

    [Fact]
    public void Show_DisplaysModalAndBackdrop_AndFocusesTheDialog()
    {
        var cut = RenderConfirmation();

        cut.InvokeAsync(() => cut.Instance.Show());

        cut.Find(".modal").GetAttribute("style").Should().Contain("display:block");
        cut.FindAll(".modal-backdrop").Should().ContainSingle();
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void Cancel_HidesModal()
    {
        var cut = RenderConfirmation();
        cut.InvokeAsync(() => cut.Instance.Show());

        cut.Find("button.btn-danger").Click();

        cut.Find(".modal").GetAttribute("style").Should().Contain("display:none");
        cut.FindAll(".modal-backdrop").Should().BeEmpty();
        _okClicks.Should().Be(0);
    }

    [Fact]
    public void CloseIcon_HidesModal()
    {
        var cut = RenderConfirmation();
        cut.InvokeAsync(() => cut.Instance.Show());

        cut.Find("button.close").Click();

        cut.FindAll(".modal-backdrop").Should().BeEmpty();
    }

    [Fact]
    public void Escape_HidesModal()
    {
        var cut = RenderConfirmation();
        cut.InvokeAsync(() => cut.Instance.Show());

        cut.Find(".modal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.FindAll(".modal-backdrop").Should().BeEmpty();
        _okClicks.Should().Be(0);
    }

    [Fact]
    public void Hide_CalledByTheOwner_HidesModal()
    {
        var cut = RenderConfirmation();
        cut.InvokeAsync(() => cut.Instance.Show());

        cut.InvokeAsync(() => cut.Instance.Hide());

        cut.FindAll(".modal-backdrop").Should().BeEmpty();
    }

    [Fact]
    public void Ok_RaisesCallback()
    {
        var cut = RenderConfirmation();

        cut.Find("button.btn-success").Click();

        _okClicks.Should().Be(1);
    }
}
