using System.Collections.ObjectModel;

namespace PictureSorter.Models;

/// <summary>
/// Represents a folder node in the folder tree hierarchy.
/// </summary>
public class FolderNode
{
    /// <summary>
    /// Gets or sets the full path of the folder.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name of the folder.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the collection of child folders.
    /// </summary>
    public ObservableCollection<FolderNode> Children { get; set; } = new();
    
    /// <summary>
    /// Gets or sets whether the folder node is expanded in the tree view.
    /// </summary>
    public bool IsExpanded { get; set; }
}
