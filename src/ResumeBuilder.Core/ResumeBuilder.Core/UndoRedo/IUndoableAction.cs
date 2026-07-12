namespace ResumeBuilder.Core.UndoRedo;

/// <summary>
/// Represents an action that can be undone and redone.
/// </summary>
public interface IUndoableAction
{
    /// <summary>
    /// Human-readable description of this action.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the action (do/redo).
    /// </summary>
    void Execute();

    /// <summary>
    /// Reverses the action (undo).
    /// </summary>
    void Undo();

    /// <summary>
    /// Timestamp when this action was created.
    /// </summary>
    DateTime CreatedAt { get; }
}

/// <summary>
/// An action that can absorb a subsequent action instead of becoming a separate history entry.
/// Typing is the motivating case: without merging, every keystroke is its own undo step.
/// </summary>
public interface IMergeableAction : IUndoableAction
{
    /// <summary>
    /// Absorbs <paramref name="next"/> if it continues this action; returns false to record it separately.
    /// </summary>
    bool TryMerge(IUndoableAction next);
}
