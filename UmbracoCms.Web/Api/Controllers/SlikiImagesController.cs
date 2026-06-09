using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using UmbracoCms.Web.Models.Sliki;
using UmbracoCms.Web.Services;
using UmbracoCms.Web.Utilities;

namespace UmbracoCms.Web.Api.Controllers;

[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/images")]
public sealed class SlikiImagesController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;
    private readonly ImageFileValidator _imageFileValidator;

    public SlikiImagesController(
        IImageStorageService imageStorageService,
        ImageFileValidator imageFileValidator)
    {
        _imageStorageService = imageStorageService;
        _imageFileValidator = imageFileValidator;
    }

    [HttpGet]
    [ProducesResponseType<ImagePageResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ImagePageResult>> GetImages(
        [FromQuery] string? searchTerm,
        [FromQuery] ImageSortBy sortBy = ImageSortBy.LatestFirst,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _imageStorageService.GetImagesAsync(
            new ImageQuery(
                searchTerm?.Trim(),
                sortBy,
                Math.Max(1, pageNumber),
                Math.Clamp(pageSize ?? _imageFileValidator.PageSize, 1, _imageFileValidator.PageSize)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("~/images/{blobName}")]
    public async Task<IActionResult> GetImage([FromRoute] string blobName, CancellationToken cancellationToken)
    {
        var image = await _imageStorageService.OpenReadAsync(blobName, cancellationToken);
        if (image is null)
        {
            return NotFound();
        }

        return File(
            image.Content,
            image.ContentType,
            lastModified: image.LastModified,
            entityTag: EntityTagHeaderValue.Parse(image.ETag),
            enableRangeProcessing: true);
    }

    [HttpPost("upload")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType<StoredImageResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"] ?? form.Files.FirstOrDefault();

        if (file is null)
        {
            return BadRequest(new { error = "Select an image file to upload." });
        }

        if (file.Length <= 0)
        {
            return BadRequest(new { error = "The selected file is empty." });
        }

        if (file.Length > _imageFileValidator.MaxUploadBytes)
        {
            return BadRequest(new
            {
                error = $"The file exceeds the limit of {FileSizeFormatter.Format(_imageFileValidator.MaxUploadBytes)}."
            });
        }

        await using var uploadedFileStream = file.OpenReadStream();
        await using var memoryStream = new MemoryStream(capacity: file.Length > int.MaxValue ? int.MaxValue : (int)file.Length);
        await uploadedFileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        byte[] headerBytes;
        if (memoryStream.TryGetBuffer(out var buffer))
        {
            headerBytes = buffer.AsSpan(0, (int)Math.Min(memoryStream.Length, 32)).ToArray();
        }
        else
        {
            var bytes = memoryStream.ToArray();
            headerBytes = bytes.AsSpan(0, Math.Min(bytes.Length, 32)).ToArray();
        }

        var validation = _imageFileValidator.Validate(file.FileName, file.ContentType, file.Length, headerBytes);
        if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedContentType))
        {
            return BadRequest(new { error = validation.ErrorMessage ?? "The image failed validation." });
        }

        memoryStream.Position = 0;
        var result = await _imageStorageService.UploadAsync(
            new UploadImageRequest(
                file.FileName,
                validation.NormalizedContentType,
                file.Length,
                memoryStream),
            progress: null,
            cancellationToken);

        return Ok(result);
    }
}
