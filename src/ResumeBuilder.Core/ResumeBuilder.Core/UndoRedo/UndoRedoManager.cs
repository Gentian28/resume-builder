using System.ComponentModel;

namespace ResumeBuilder.Core.UndoRedo;

/// <summary>
/// Manages undo/redo operations with a stack-based approach.
/// </summary>
public class UndoRedoManager : INotifyPropertyChanged
{
    private readonly Stack<IUndoableAction> _undoStack = new();
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

    public string? NextUndoDescription => _undoStack.TryPeek(out var action) ? action.Description : null;
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
            _undoStack.Push(action);
            _redoStack.Clear();

            // Trim history if needed
            TrimHistory();

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

        _undoStack.Push(action);
        _redoStack.Clear();
        TrimHistory();

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
            var action = _undoStack.Pop();
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
            _undoStack.Push(action);

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

    private void TrimHistory()
    {
        while (_undoStack.Count > _maxHistorySize)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < _maxHistorySize; i++)
            {
                _undoStack.Push(items[_maxHistorySize - 1 - i]);
            }
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
