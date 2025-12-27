namespace PictureSorter.Commands;

/// <summary>
/// Represents a command that can be undone and redone.
/// </summary>
public interface IUndoableCommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <returns>True if execution was successful; otherwise false.</returns>
    Task<bool> ExecuteAsync();
    
    /// <summary>
    /// Undoes the command, reversing its effects.
    /// </summary>
    /// <returns>True if undo was successful; otherwise false.</returns>
    Task<bool> UndoAsync();
    
    /// <summary>
    /// Gets a description of the command for display purposes.
    /// </summary>
    string Description { get; }
}
