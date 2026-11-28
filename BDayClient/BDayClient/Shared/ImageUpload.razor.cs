using System.Net.Http.Headers;
using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace BDayClient.Shared;

public partial class ImageUpload
{
    private const string NoFileMessage = "No file chosen";

    // The image is resized in the browser, so the upload stays far below the server's limit.
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private string _fileUploadMessage = NoFileMessage;
    private string? _imageUrl;
    private string? _lastImageUrlParameter;

    /// <summary>The current image: a path returned by the upload or an absolute URL (older records).</summary>
    [Parameter]
    public string? ImageUrl { get; set; }

    /// <summary>Raised with the stored path of a newly uploaded image.</summary>
    [Parameter]
    public EventCallback<string> OnChange { get; set; }

    [Inject]
    public IPersonHttpRepository Repository { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [Inject]
    private ApiUrls ApiUrls { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (ImageUrl == _lastImageUrlParameter)
            return;

        _lastImageUrlParameter = ImageUrl;
        _imageUrl = ImageUrl;
    }

    private async Task HandleSelected(InputFileChangeEventArgs e)
    {
        var imageFile = e.File;
        _fileUploadMessage = imageFile.Name;

        try
        {
            var resizedFile = await imageFile.RequestImageFileAsync("image/png", 300, 500);

            await using var stream = resizedFile.OpenReadStream(MaxUploadBytes);
            using var content = CreateUploadContent(stream, imageFile.Name);

            _imageUrl = await Repository.UploadPersonImage(content);
            await OnChange.InvokeAsync(_imageUrl);
        }
        catch (ApiException ex)
        {
            _fileUploadMessage = NoFileMessage;
            ToastService.ShowApiError(ex);
        }
        catch (Exception ex) when (ex is JSException or IOException)
        {
            // RequestImageFileAsync fails for files the browser cannot read as an image.
            _fileUploadMessage = NoFileMessage;
            ToastService.ShowError($"\"{imageFile.Name}\" could not be read as an image. Please choose an image file.");
        }
    }

    /// <summary>The upload request for an image the browser has converted to PNG.</summary>
    internal static MultipartFormDataContent CreateUploadContent(Stream pngImage, string originalFileName)
    {
        var fileContent = new StreamContent(pngImage);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        // The bytes are PNG now, so the name must say so too: the server accepts uploads by their extension
        // (a .jfif, .avif or .heic name would be refused although the content is a PNG).
        var content = new MultipartFormDataContent();
        content.Add(fileContent, "image", Path.ChangeExtension(originalFileName, ".png"));
        return content;
    }
}
