using Contracts.Exceptions;
using Core.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BDayServer.UnitTests.Core;

public sealed class UploadManagerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bday-upload-tests", Guid.NewGuid().ToString("N"));
    private readonly UploadManager _sut;

    public UploadManagerTests()
    {
        _sut = new UploadManager(Options.Create(new UploadOptions { RootPath = _root }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static HttpRequest FormRequest(params IFormFile[] files)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "multipart/form-data; boundary=test";
        var fileCollection = new FormFileCollection();
        fileCollection.AddRange(files);
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), fileCollection);
        return context.Request;
    }

    private static FormFile File(string fileName, byte[] content, long? length = null) =>
        new(new MemoryStream(content), 0, length ?? content.Length, "file", fileName);

    private string[] StoredFiles() =>
        Directory.Exists(Path.Combine(_root, "Images")) ? Directory.GetFiles(Path.Combine(_root, "Images")) : [];

    [Fact]
    public async Task NotAForm_IsRejected()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";

        var act = () => _sut.UploadAsync(context.Request);

        await act.Should().ThrowAsync<UploadException>();
    }

    [Fact]
    public async Task NoFile_IsRejected()
    {
        var act = () => _sut.UploadAsync(FormRequest());

        await act.Should().ThrowAsync<UploadException>();
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("drawing.svg")]
    [InlineData("noextension")]
    public async Task NonImageFile_IsRejectedAndNotStored(string fileName)
    {
        var act = () => _sut.UploadAsync(FormRequest(File(fileName, [1, 2, 3])));

        await act.Should().ThrowAsync<UploadException>();
        StoredFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task TooLargeFile_IsRejected()
    {
        var act = () => _sut.UploadAsync(FormRequest(File("big.png", [1], length: UploadManager.MaxFileSizeBytes + 1)));

        (await act.Should().ThrowAsync<UploadException>()).WithMessage("*too large*");
    }

    [Fact]
    public async Task Image_IsStoredUnderANewNameAndItsUrlPathReturned()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47];

        var urlPath = await _sut.UploadAsync(FormRequest(File(@"..\..\Photo.PNG", content)), TestContext.Current.CancellationToken);

        urlPath.Should().MatchRegex("^StaticFiles/Images/[0-9a-f]{32}\\.png$");
        var stored = StoredFiles().Should().ContainSingle().Subject;
        Path.GetFileName(stored).Should().Be(urlPath["StaticFiles/Images/".Length..]);
        (await System.IO.File.ReadAllBytesAsync(stored, TestContext.Current.CancellationToken)).Should().Equal(content);
    }
}
