using System.IO;
using PictureSorter.Services;

namespace PictureSorter.Commands;

/// <summary>
/// Command to copy multiple images to a target folder.
/// </summary>
public class CopyImagesCommand : IUndoableCommand
{
    private readonly IImageService _imageService;
    private readonly List<string> _sourceFilePaths;
    private readonly string _targetFolderPath;
    private readonly List<string> _copiedFiles = new();
    private readonly List<string> _successfulSourceFiles = new();
    
    public string Description => $"Copy {_sourceFilePaths.Count} image(s)";
    
    /// <summary>
    /// Gets the list of source files that were successfully copied (for state restoration on undo).
    /// </summary>
    public IReadOnlyList<string> SuccessfulSourceFiles => _successfulSourceFiles;

    public CopyImagesCommand(IImageService imageService, IEnumerable<string> sourceFilePaths, string targetFolderPath)
    {
        _imageService = imageService;
        _sourceFilePaths = sourceFilePaths.ToList();
        _targetFolderPath = targetFolderPath;
    }

    public async Task<bool> ExecuteAsync()
    {
        _copiedFiles.Clear();
        _successfulSourceFiles.Clear();
        var anySuccess = false;

        foreach (var filePath in _sourceFilePaths)
        {
            try
            {
                var success = await _imageService.CopyImageAsync(filePath, _targetFolderPath);
                if (success)
                {
                    var targetPath = Path.Combine(_targetFolderPath, Path.GetFileName(filePath));
                    _copiedFiles.Add(targetPath);
                    _successfulSourceFiles.Add(filePath);
                    anySuccess = true;
                }
            }
            catch
            {
                // Continue with other files
            }
        }

        return anySuccess;
    }

    public async Task<bool> UndoAsync()
    {
        var anySuccess = false;

        foreach (var targetPath in _copiedFiles)
        {
            try
            {
                if (File.Exists(targetPath))
                {
                    await Task.Run(() => File.Delete(targetPath));
                    anySuccess = true;
                }
            }
            catch
            {
                // Continue with other files
            }
        }

        return anySuccess;
    }
}
