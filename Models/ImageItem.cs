using System.Windows.Media.Imaging;
using PictureSorter.ViewModels;

namespace PictureSorter.Models;

/// <summary>
/// Represents an image item with its metadata and UI state.
/// </summary>
public class ImageItem : ViewModelBase
{
    private bool _isChecked;
    private BitmapSource? _thumbnail;

    /// <summary>
    /// Gets or sets the full file path of the image.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the file name of the image.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the creation date of the image.
    /// </summary>
    public DateTime? CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail image for display.
    /// </summary>
    public BitmapSource? Thumbnail
    {
        get => _thumbnail;
        set => SetProperty(ref _thumbnail, value);
    }

    /// <summary>
    /// Gets or sets whether the image is checked for copying to target folder.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }
}
