using System.Reflection;

namespace ResumeBuilder.Core.UndoRedo;

/// <summary>
/// Represents a property value change that can be undone.
/// </summary>
public class PropertyChangeAction : IUndoableAction
{
    private readonly object _target;
    private readonly PropertyInfo _property;
    private readonly object? _oldValue;
    private readonly object? _newValue;
    private readonly Action? _onChanged;

    public string Description { get; }
    public DateTime CreatedAt { get; }

    public PropertyChangeAction(
        object target,
        string propertyName,
        object? oldValue,
        object? newValue,
        string? description = null,
        Action? onChanged = null)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _property = target.GetType().GetProperty(propertyName)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{target.GetType().Name}'");
        _oldValue = oldValue;
        _newValue = newValue;
        _onChanged = onChanged;
        Description = description ?? $"Change {propertyName}";
        CreatedAt = DateTime.Now;
    }

    public void Execute()
    {
        _property.SetValue(_target, _newValue);
        _onChanged?.Invoke();
    }

    public void Undo()
    {
        _property.SetValue(_target, _oldValue);
        _onChanged?.Invoke();
    }
}

/// <summary>
/// Represents a property value change using getter/setter delegates instead of reflection.
/// More performant and type-safe than PropertyChangeAction.
/// </summary>
public class PropertyChangeAction<T> : IUndoableAction
{
    private readonly Action<T?> _setter;
    private readonly T? _oldValue;
    private readonly T? _newValue;
    private readonly Action? _onChanged;

    public string Description { get; }
    public DateTime CreatedAt { get; }

    public PropertyChangeAction(
        Action<T?> setter,
        T? oldValue,
        T? newValue,
        string description,
        Action? onChanged = null)
    {
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        _oldValue = oldValue;
        _newValue = newValue;
        _onChanged = onChanged;
        Description = description;
        CreatedAt = DateTime.Now;
    }

    public void Execute()
    {
        _setter(_newValue);
        _onChanged?.Invoke();
    }

    public void Undo()
    {
        _setter(_oldValue);
        _onChanged?.Invoke();
    }
}
