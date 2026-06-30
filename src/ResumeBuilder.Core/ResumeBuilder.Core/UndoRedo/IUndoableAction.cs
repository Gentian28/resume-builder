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
