using System.ComponentModel.DataAnnotations;
using BDayClient.Pocos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BDayClient.Components;

/// <summary>
/// The person form used by the create and update pages. Passing a new <see cref="Model"/> instance starts over with a
/// clean form (e.g. after a successful save).
/// </summary>
public partial class PersonForm : IDisposable
{
    private EditContext? _editContext;
    private bool _canSubmit;
    private bool _submitting;

    [Parameter, EditorRequired]
    public PersonForManipulation Model { get; set; } = default!;

    [Parameter, EditorRequired]
    public string SubmitText { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_editContext?.Model, Model))
            return;

        DetachEditContext();
        _editContext = new EditContext(Model);
        _editContext.OnFieldChanged += HandleFieldChanged;
        _canSubmit = false;
    }

    // Validates silently: the messages only appear for fields the user has touched (and for all of them on submit).
    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) =>
        _canSubmit = _editContext!.IsModified() &&
                     Validator.TryValidateObject(Model, new ValidationContext(Model), null, validateAllProperties: true);

    private async Task HandleValidSubmit()
    {
        _submitting = true;
        try
        {
            await OnValidSubmit.InvokeAsync();
        }
        finally
        {
            _submitting = false;
        }
    }

    private void AssignImageUrl(string imageUrl)
    {
        Model.ImageUrl = imageUrl;
        _editContext?.NotifyFieldChanged(_editContext.Field(nameof(PersonForManipulation.ImageUrl)));
    }

    private void DetachEditContext()
    {
        if (_editContext is not null)
            _editContext.OnFieldChanged -= HandleFieldChanged;
    }

    public void Dispose() => DetachEditContext();
}
