using System.Windows.Media.Imaging;

namespace PictureSorter.Services;

/// <summary>
/// Service interface for image operations including loading, copying, rotating, and deleting.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Loads a thumbnail image with default width.
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <returns>A BitmapSource containing the thumbnail, or null if loading fails.</returns>
    Task<BitmapSource?> LoadThumbnailAsync(string filePath);
    
    /// <summary>
    /// Loads a thumbnail image with specified width, applying EXIF orientation.
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <param name="thumbnailWidth">The desired width of the thumbnail in pixels.</param>
    /// <returns>A BitmapSource containing the thumbnail, or null if loading fails.</returns>
    Task<BitmapSource?> LoadThumbnailAsync(string filePath, int thumbnailWidth);
    
    /// <summary>
    /// Copies an image file to the target folder.
    /// </summary>
    /// <param name="sourceFilePath">The source image file path.</param>
    /// <param name="targetFolderPath">The destination folder path.</param>
    /// <returns>True if the copy was successful; otherwise false.</returns>
    Task<bool> CopyImageAsync(string sourceFilePath, string targetFolderPath);
    Task<bool> MoveImageAsync(string sourceFilePath, string targetFolderPath);
    
    /// <summary>
    /// Gets all image files from the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder path to scan.</param>
    /// <returns>A list of image file paths.</returns>
    List<string> GetImageFiles(string folderPath);
    
    /// <summary>
    /// Determines if a file is a supported image format.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a supported image format; otherwise false.</returns>
    bool IsImageFile(string filePath);
    
    /// <summary>
    /// Rotates an image by the specified degrees and saves it.
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <param name="degrees">The rotation angle in degrees (90, 180, or 270).</param>
    /// <returns>True if rotation was successful; otherwise false.</returns>
    Task<bool> RotateImageAsync(string filePath, int degrees);
    
    /// <summary>
    /// Deletes an image file.
    /// </summary>
    /// <param name="filePath">The path to the image file to delete.</param>
    /// <returns>True if deletion was successful; otherwise false.</returns>
    Task<bool> DeleteImageAsync(string filePath);
    
    /// <summary>
    /// Gets GPS coordinates from an image's EXIF data.
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <returns>A tuple containing latitude and longitude if available; otherwise null.</returns>
    Task<(double? Latitude, double? Longitude)?> GetGpsCoordinatesAsync(string filePath);
}
