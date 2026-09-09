namespace ResumeBuilder.Core.Editing;

/// <summary>
/// Tracks edits across an asynchronous save so that an edit made while the write is in flight is
/// never marked as saved.
///
/// A save reads the editor, awaits the database, then clears the dirty flag. Anything typed during
/// that await was not in what was written, and clearing the flag afterwards tells the user "all
/// changes saved" about a change that is only in memory. The generation counter makes the clear
/// conditional: a save only settles the edits it actually started with.
/// </summary>
public sealed class DirtyTracker
{
    private long _generation;

    /// <summary>Whether there are edits no completed save has covered.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Records an edit. Every call moves the generation, so an in-flight save cannot claim it.</summary>
    public void MarkDirty()
    {
        IsDirty = true;
        _generation++;
    }

    /// <summary>Call before reading the editor for a save. The token is what <see cref="CompleteSave"/> settles.</summary>
    public long BeginSave() => _generation;

    /// <summary>
    /// Call after the write succeeded. Returns true, and clears the flag, only when nothing was edited
    /// since <see cref="BeginSave"/>; otherwise the tracker stays dirty so the next save carries the
    /// newer edits.
    /// </summary>
    public bool CompleteSave(long token)
    {
        if (token != _generation)
            return false;

        IsDirty = false;
        return true;
    }

    /// <summary>The document was replaced (loaded, created, reloaded): nothing pending, and any in-flight save is stale.</summary>
    public void Reset()
    {
        IsDirty = false;
        _generation++;
    }
}
