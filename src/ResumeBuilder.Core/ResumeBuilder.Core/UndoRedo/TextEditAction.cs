namespace ResumeBuilder.Core.UndoRedo;

/// <summary>
/// A change to one text field, recorded so it can be undone. Consecutive edits to the same field
/// within <see cref="MergeWindow"/> collapse into a single history entry, so Ctrl+Z steps back over
/// a phrase rather than one character at a time.
/// </summary>
public class TextEditAction : IMergeableAction
{
    /// <summary>How long an edit stays open to absorb the next one in the same field.</summary>
    public static readonly TimeSpan MergeWindow = TimeSpan.FromSeconds(2);

    private readonly Action<string> _setter;
    private readonly Action? _onChanged;

    /// <summary>Identifies the field, so edits to different fields never merge into each other.</summary>
    public string FieldKey { get; }

    public string OldValue { get; }
    public string NewValue { get; private set; }

    public string Description { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastEditedAt { get; private set; }

    public TextEditAction(
        string fieldKey,
        string oldValue,
        string newValue,
        Action<string> setter,
        string? description = null,
        Action? onChanged = null)
    {
        FieldKey = fieldKey;
        OldValue = oldValue;
        NewValue = newValue;
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        _onChanged = onChanged;
        Description = description ?? $"Edit {fieldKey}";
        CreatedAt = DateTime.Now;
        LastEditedAt = CreatedAt;
    }

    public void Execute()
    {
        _setter(NewValue);
        _onChanged?.Invoke();
    }

    public void Undo()
    {
        _setter(OldValue);
        _onChanged?.Invoke();
    }

    public bool TryMerge(IUndoableAction next)
    {
        if (next is not TextEditAction edit)
            return false;

        if (!string.Equals(edit.FieldKey, FieldKey, StringComparison.Ordinal))
            return false;

        if (edit.CreatedAt - LastEditedAt > MergeWindow)
            return false;

        // Keep this action's OldValue (the state before the burst of typing began) and take the
        // latest NewValue, so undoing once reverts the whole burst.
        NewValue = edit.NewValue;
        LastEditedAt = edit.CreatedAt;
        return true;
    }
}
