using BDayClient.Features;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class Pagination
{
    [Parameter, EditorRequired]
    public MetaData MetaData { get; set; } = new();

    [Parameter]
    public int Spread { get; set; }

    [Parameter]
    public EventCallback<int> SelectedPage { get; set; }

    private List<PagingLink> _links = [];

    protected override void OnParametersSet()
    {
        CreatePaginationLinks();
    }

    private void CreatePaginationLinks()
    {
        _links = [new PagingLink(MetaData.CurrentPage - 1, MetaData.HasPrevious, "Previous")];

        for (var i = 1; i <= MetaData.TotalPages; i++)
        {
            if (i >= MetaData.CurrentPage - Spread && i <= MetaData.CurrentPage + Spread)
                _links.Add(new PagingLink(i, true, i.ToString())
                {
                    Active = MetaData.CurrentPage == i
                });
        }

        _links.Add(new PagingLink(MetaData.CurrentPage + 1, MetaData.HasNext, "Next"));
    }

    // The parent loads the page and passes the new MetaData back; this component does not change it itself.
    private async Task OnSelectedPage(PagingLink link)
    {
        if (link.Page == MetaData.CurrentPage || !link.Enabled)
            return;

        await SelectedPage.InvokeAsync(link.Page);
    }
}
