using System.Collections.ObjectModel;

namespace PictureSorter.Commands;

/// <summary>
/// Manages undo and redo operations for undoable commands.
/// </summary>
public class UndoRedoManager
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private readonly int _maxUndoLevels;

    /// <summary>
    /// Gets whether there are commands available to undo.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;
    
    /// <summary>
    /// Gets whether there are commands available to redo.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;
    
    /// <summary>
    /// Gets the description of the next command that would be undone.
    /// </summary>
    public string? UndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;
    
    /// <summary>
    /// Gets the description of the next command that would be redone.
    /// </summary>
    public string? RedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;
    
    /// <summary>
    /// Gets the most recently undone command (for state restoration purposes).
    /// </summary>
    public IUndoableCommand? LastUndoneCommand => _redoStack.Count > 0 ? _redoStack.Peek() : null;

    /// <summary>
    /// Event raised when the undo/redo state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Initializes a new instance of the UndoRedoManager class.
    /// </summary>
    /// <param name="maxUndoLevels">Maximum number of undo levels to maintain (default: 50).</param>
    public UndoRedoManager(int maxUndoLevels = 50)
    {
        _maxUndoLevels = maxUndoLevels;
    }

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>True if execution was successful; otherwise false.</returns>
    public async Task<bool> ExecuteCommandAsync(IUndoableCommand command)
    {
        var success = await command.ExecuteAsync();
        
        if (success)
        {
            _undoStack.Push(command);
            
            // Limit undo stack size
            if (_undoStack.Count > _maxUndoLevels)
            {
                var tempStack = new Stack<IUndoableCommand>(_undoStack.Reverse().Take(_maxUndoLevels).Reverse());
                _undoStack.Clear();
                foreach (var cmd in tempStack)
                {
                    _undoStack.Push(cmd);
                }
            }
            
            // Clear redo stack when a new command is executed
            _redoStack.Clear();
            
            OnStateChanged();
        }
        
        return success;
    }

    /// <summary>
    /// Undoes the most recent command.
    /// </summary>
    /// <returns>True if undo was successful; otherwise false.</returns>
    public async Task<bool> UndoAsync()
    {
        if (!CanUndo)
            return false;

        var command = _undoStack.Pop();
        var success = await command.UndoAsync();
        
        if (success)
        {
            _redoStack.Push(command);
            OnStateChanged();
        }
        else
        {
            // If undo fails, put the command back
            _undoStack.Push(command);
        }
        
        return success;
    }

    /// <summary>
    /// Redoes the most recently undone command.
    /// </summary>
    /// <returns>True if redo was successful; otherwise false.</returns>
    public async Task<bool> RedoAsync()
    {
        if (!CanRedo)
            return false;

        var command = _redoStack.Pop();
        var success = await command.ExecuteAsync();
        
        if (success)
        {
            _undoStack.Push(command);
            OnStateChanged();
        }
        else
        {
            // If redo fails, put the command back
            _redoStack.Push(command);
        }
        
        return success;
    }

    /// <summary>
    /// Clears all undo and redo history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
