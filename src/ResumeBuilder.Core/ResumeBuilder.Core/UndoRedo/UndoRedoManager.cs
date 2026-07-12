using System.ComponentModel;

namespace ResumeBuilder.Core.UndoRedo;

/// <summary>
/// Manages undo/redo operations with a stack-based approach.
/// </summary>
public class UndoRedoManager : INotifyPropertyChanged
{
    // A linked list rather than a Stack: trimming the oldest entry is O(1) here, where popping the
    // bottom of a Stack meant rebuilding the whole thing on every push past the limit.
    private readonly LinkedList<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private readonly int _maxHistorySize;
    private bool _isExecutingAction;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<IUndoableAction>? ActionExecuted;
    public event Action<IUndoableAction>? ActionUndone;
    public event Action<IUndoableAction>? ActionRedone;

    public UndoRedoManager(int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public string? NextUndoDescription => _undoStack.First?.Value.Description;
    public string? NextRedoDescription => _redoStack.TryPeek(out var action) ? action.Description : null;

    /// <summary>
    /// Indicates if we're currently executing an undo/redo action.
    /// Use this to prevent nested recording.
    /// </summary>
    public bool IsExecutingAction => _isExecutingAction;

    /// <summary>
    /// Executes an action and adds it to the undo stack.
    /// </summary>
    public void Execute(IUndoableAction action)
    {
        if (_isExecutingAction)
            return;

        try
        {
            _isExecutingAction = true;
            action.Execute();
            PushUndo(action);
            _redoStack.Clear();

            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoCount));
            OnPropertyChanged(nameof(RedoCount));
            OnPropertyChanged(nameof(NextUndoDescription));
            OnPropertyChanged(nameof(NextRedoDescription));

            ActionExecuted?.Invoke(action);
        }
        finally
        {
            _isExecutingAction = false;
        }
    }

    /// <summary>
    /// Records an already-executed action to the undo stack without executing it.
    /// Useful for tracking changes that happen elsewhere.
    /// </summary>
    public void RecordAction(IUndoableAction action)
    {
        if (_isExecutingAction)
            return;

        PushUndo(action);
        _redoStack.Clear();

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoCount));
        OnPropertyChanged(nameof(RedoCount));
        OnPropertyChanged(nameof(NextUndoDescription));
        OnPropertyChanged(nameof(NextRedoDescription));
    }

    /// <summary>
    /// Undoes the most recent action.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo || _isExecutingAction)
            return;

        try
        {
            _isExecutingAction = true;
            var action = _undoStack.First!.Value;
            _undoStack.RemoveFirst();
            action.Undo();
            _redoStack.Push(action);

            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoCount));
            OnPropertyChanged(nameof(RedoCount));
            OnPropertyChanged(nameof(NextUndoDescription));
            OnPropertyChanged(nameof(NextRedoDescription));

            ActionUndone?.Invoke(action);
        }
        finally
        {
            _isExecutingAction = false;
        }
    }

    /// <summary>
    /// Redoes the most recently undone action.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo || _isExecutingAction)
            return;

        try
        {
            _isExecutingAction = true;
            var action = _redoStack.Pop();
            action.Execute();
            _undoStack.AddFirst(action);

            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoCount));
            OnPropertyChanged(nameof(RedoCount));
            OnPropertyChanged(nameof(NextUndoDescription));
            OnPropertyChanged(nameof(NextRedoDescription));

            ActionRedone?.Invoke(action);
        }
        finally
        {
            _isExecutingAction = false;
        }
    }

    /// <summary>
    /// Clears all undo/redo history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoCount));
        OnPropertyChanged(nameof(RedoCount));
        OnPropertyChanged(nameof(NextUndoDescription));
        OnPropertyChanged(nameof(NextRedoDescription));
    }

    /// <summary>
    /// Gets the undo history (most recent first).
    /// </summary>
    public IEnumerable<IUndoableAction> GetUndoHistory()
    {
        return _undoStack.ToArray();
    }

    /// <summary>
    /// Gets the redo history (most recent first).
    /// </summary>
    public IEnumerable<IUndoableAction> GetRedoHistory()
    {
        return _redoStack.ToArray();
    }

    /// <summary>
    /// Adds to the undo history, letting the most recent action absorb this one when it can
    /// (consecutive keystrokes in one field), and dropping the oldest entry past the limit.
    /// </summary>
    private void PushUndo(IUndoableAction action)
    {
        if (_undoStack.First?.Value is IMergeableAction mergeable && mergeable.TryMerge(action))
        {
            return;
        }

        _undoStack.AddFirst(action);

        while (_undoStack.Count > _maxHistorySize)
        {
            _undoStack.RemoveLast();
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
