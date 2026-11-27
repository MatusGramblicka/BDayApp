using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BDayClient.Shared;

public partial class Confirmation
{
    private readonly string _titleId = $"confirmation-title-{Guid.NewGuid():N}";

    private string _modalDisplay = "none;";
    private bool _showBackdrop;
    private bool _focusDialog;
    private ElementReference _dialog;

    [Parameter]
    public string BodyMessage { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnOKClicked { get; set; }

    /// <summary>Opens the dialog. Called by the owning component through @ref.</summary>
    public void Show()
    {
        _modalDisplay = "block;";
        _showBackdrop = true;
        _focusDialog = true;
        StateHasChanged();
    }

    /// <summary>Closes the dialog. Called by the owning component through @ref.</summary>
    public void Hide()
    {
        Close();
        StateHasChanged();
    }

    // Used by the dialog's own buttons, whose event handling re-renders anyway.
    private void Close()
    {
        _modalDisplay = "none;";
        _showBackdrop = false;
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
            Close();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusDialog)
            return;

        // Move the focus into the dialog, so keyboard users land there and Escape closes it.
        _focusDialog = false;
        await _dialog.FocusAsync();
    }
}
