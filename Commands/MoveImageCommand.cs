using System.IO;
using PictureSorter.Services;

namespace PictureSorter.Commands;

/// <summary>
/// Command to move an image file from source to target folder.
/// </summary>
public class MoveImageCommand : IUndoableCommand
{
    private readonly IImageService _imageService;
    private readonly string _sourceFilePath;
    private readonly string _targetFolderPath;
    private string? _targetFilePath;
    
    public string Description => $"Move {Path.GetFileName(_sourceFilePath)}";
    
    /// <summary>
    /// Gets the source file path for state restoration.
    /// </summary>
    public string SourceFilePath => _sourceFilePath;

    public MoveImageCommand(IImageService imageService, string sourceFilePath, string targetFolderPath)
    {
        _imageService = imageService;
        _sourceFilePath = sourceFilePath;
        _targetFolderPath = targetFolderPath;
    }

    public async Task<bool> ExecuteAsync()
    {
        var success = await _imageService.MoveImageAsync(_sourceFilePath, _targetFolderPath);
        if (success)
        {
            _targetFilePath = Path.Combine(_targetFolderPath, Path.GetFileName(_sourceFilePath));
        }
        return success;
    }

    public async Task<bool> UndoAsync()
    {
        if (string.IsNullOrEmpty(_targetFilePath) || !File.Exists(_targetFilePath))
            return false;

        var sourceDirectory = Path.GetDirectoryName(_sourceFilePath);
        if (string.IsNullOrEmpty(sourceDirectory))
            return false;

        return await _imageService.MoveImageAsync(_targetFilePath, sourceDirectory);
    }
}
