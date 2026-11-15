using BDayClient.Shared;

namespace BDayClient.UnitTests.Components;

public class ConfirmationTests : TestContext
{
    private int _okClicks;

    private IRenderedComponent<Confirmation> Render() =>
        RenderComponent<Confirmation>(ps => ps
            .Add(p => p.BodyMessage, "Delete Anna?")
            .Add(p => p.OnOKClicked, () => _okClicks++));

    [Fact]
    public void Renders_BodyMessage_AndIsHiddenInitially()
    {
        var cut = Render();

        cut.Find(".modal-body p").TextContent.Should().Be("Delete Anna?");
        cut.FindAll(".modal-backdrop").Should().BeEmpty();
        cut.Find(".modal").GetAttribute("style").Should().NotContain("block");
    }

    [Fact]
    public void Show_DisplaysModalAndBackdrop()
    {
        var cut = Render();

        cut.InvokeAsync(() => cut.Instance.Show());

        cut.Find(".modal").GetAttribute("style").Should().Contain("display:block");
        cut.FindAll(".modal-backdrop").Should().ContainSingle();
    }

    [Fact]
    public void Cancel_HidesModal()
    {
        var cut = Render();
        cut.InvokeAsync(() => cut.Instance.Show());

        cut.Find("button.btn-danger").Click();

        cut.Find(".modal").GetAttribute("style").Should().Contain("display:none");
        cut.FindAll(".modal-backdrop").Should().BeEmpty();
        _okClicks.Should().Be(0);
    }

    [Fact]
    public void CloseIcon_HidesModal()
    {
        var cut = Render();
        cut.InvokeAsync(() => cut.Instance.Show());

        cut.Find("button.close").Click();

        cut.FindAll(".modal-backdrop").Should().BeEmpty();
    }

    [Fact]
    public void Ok_RaisesCallback()
    {
        var cut = Render();

        cut.Find("button.btn-success").Click();

        _okClicks.Should().Be(1);
    }
}
