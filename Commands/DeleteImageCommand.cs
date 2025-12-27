using System.IO;
using PictureSorter.Services;

namespace PictureSorter.Commands;

/// <summary>
/// Command to delete an image file.
/// </summary>
public class DeleteImageCommand : IUndoableCommand
{
    private readonly IImageService _imageService;
    private readonly string _filePath;
    private byte[]? _fileBackup;
    
    public string Description => $"Delete {Path.GetFileName(_filePath)}";
    
    /// <summary>
    /// Gets the file path for state restoration.
    /// </summary>
    public string FilePath => _filePath;

    public DeleteImageCommand(IImageService imageService, string filePath)
    {
        _imageService = imageService;
        _filePath = filePath;
    }

    public async Task<bool> ExecuteAsync()
    {
        try
        {
            // Backup the file before deleting
            if (File.Exists(_filePath))
            {
                _fileBackup = await File.ReadAllBytesAsync(_filePath);
                return await _imageService.DeleteImageAsync(_filePath);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UndoAsync()
    {
        try
        {
            if (_fileBackup == null)
                return false;

            // Restore the file from backup
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(_filePath, _fileBackup);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
