using System.IO;
using PictureSorter.Services;

namespace PictureSorter.Commands;

/// <summary>
/// Command to rotate an image by a specified angle.
/// </summary>
public class RotateImageCommand : IUndoableCommand
{
    private readonly IImageService _imageService;
    private readonly string _filePath;
    private readonly int _degrees;
    
    public string Description => $"Rotate {Path.GetFileName(_filePath)} {_degrees}°";

    public RotateImageCommand(IImageService imageService, string filePath, int degrees)
    {
        _imageService = imageService;
        _filePath = filePath;
        _degrees = degrees;
    }

    public async Task<bool> ExecuteAsync()
    {
        return await _imageService.RotateImageAsync(_filePath, _degrees);
    }

    public async Task<bool> UndoAsync()
    {
        // Rotate in the opposite direction
        var reverseDegrees = -_degrees;
        return await _imageService.RotateImageAsync(_filePath, reverseDegrees);
    }
}
