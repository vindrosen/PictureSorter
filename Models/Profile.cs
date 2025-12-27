namespace PictureSorter.Models;

/// <summary>
/// Represents a profile that stores workspace state including folders, processed images, and selections.
/// </summary>
public class Profile
{
    /// <summary>
    /// Gets or sets the unique name of the profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root folder path for this profile.
    /// </summary>
    public string? RootFolderPath { get; set; }

    /// <summary>
    /// Gets or sets the target folder path for this profile.
    /// </summary>
    public string? TargetFolderPath { get; set; }

    /// <summary>
    /// Gets or sets the set of processed image file paths.
    /// </summary>
    public HashSet<string> ProcessedImages { get; set; } = new();

    /// <summary>
    /// Gets or sets the set of selected image file paths for batch operations.
    /// </summary>
    public HashSet<string> SelectedImages { get; set; } = new();

    /// <summary>
    /// Gets or sets the images per page setting.
    /// </summary>
    public int ImagesPerPage { get; set; } = 5;

    /// <summary>
    /// Gets or sets the thumbnail size setting.
    /// </summary>
    public int ThumbnailSize { get; set; } = 150;

    /// <summary>
    /// Gets or sets the sort option.
    /// </summary>
    public string SortBy { get; set; } = "CreationDate";

    /// <summary>
    /// Gets or sets whether sorting is in descending order.
    /// </summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation date of the profile.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the last modified date of the profile.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;
}
