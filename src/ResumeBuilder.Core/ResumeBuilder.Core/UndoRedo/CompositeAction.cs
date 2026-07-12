namespace ResumeBuilder.Core.UndoRedo;

/// <summary>
/// Groups multiple actions into a single undoable action.
/// Useful for batch operations that should be treated as one step.
/// </summary>
public class CompositeAction : IUndoableAction
{
    private readonly List<IUndoableAction> _actions;

    public string Description { get; }
    public DateTime CreatedAt { get; }

    public CompositeAction(string description, params IUndoableAction[] actions)
        : this(description, (IEnumerable<IUndoableAction>)actions)
    {
    }

    public CompositeAction(string description, IEnumerable<IUndoableAction> actions)
    {
        Description = description;
        _actions = actions.ToList();
        CreatedAt = DateTime.Now;
    }

    public void Execute()
    {
        // Roll back on partial failure. Without this, a composite that throws halfway leaves the
        // first few actions applied but never reaches the undo stack, so they can't be taken back.
        var applied = new List<IUndoableAction>();

        try
        {
            foreach (var action in _actions)
            {
                action.Execute();
                applied.Add(action);
            }
        }
        catch
        {
            for (int i = applied.Count - 1; i >= 0; i--)
            {
                try
                {
                    applied[i].Undo();
                }
                catch
                {
                    // Already failing; keep unwinding the rest rather than masking the original error.
                }
            }

            throw;
        }
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _actions.Count - 1; i >= 0; i--)
        {
            _actions[i].Undo();
        }
    }

    public void Add(IUndoableAction action)
    {
        _actions.Add(action);
    }

    public int Count => _actions.Count;
}

/// <summary>
/// Builder for creating composite actions.
/// </summary>
public class CompositeActionBuilder
{
    private readonly List<IUndoableAction> _actions = new();
    private string _description = "Multiple changes";

    public CompositeActionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CompositeActionBuilder Add(IUndoableAction action)
    {
        _actions.Add(action);
        return this;
    }

    public CompositeActionBuilder AddPropertyChange<T>(
        Action<T?> setter,
        T? oldValue,
        T? newValue,
        string description,
        Action? onChanged = null)
    {
        _actions.Add(new PropertyChangeAction<T>(setter, oldValue, newValue, description, onChanged));
        return this;
    }

    public CompositeAction Build()
    {
        return new CompositeAction(_description, _actions);
    }

    public bool HasActions => _actions.Count > 0;
}
