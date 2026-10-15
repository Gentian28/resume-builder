namespace ResumeBuilder.Core.Editing;

/// <summary>Coalesces edits, serializes writes, and retains the final failed edit for retry.</summary>
public sealed class PendingSave<T>(Func<T> snapshot, Func<T, Task<T>> write, Action<T> acceptMetadata,
    Action changed, TimeSpan? debounce = null)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _generation;
    private long _savedGeneration;
    private Task? _scheduled;
    private bool _stopped;

    public bool HasChanges => _generation != _savedGeneration;
    public string? Error { get; private set; }

    public void MarkDirty()
    {
        if (_stopped) return;
        _generation++;
        changed();
        _scheduled ??= SaveLaterAsync();
    }

    private async Task SaveLaterAsync()
    {
        await Task.Delay(debounce ?? TimeSpan.FromMilliseconds(350));
        await FlushAsync();
        _scheduled = null;
        if (!_stopped && HasChanges && Error is null)
            _scheduled = SaveLaterAsync();
    }

    public async Task<bool> FlushAsync()
    {
        await _gate.WaitAsync();
        try
        {
            while (!_stopped && HasChanges)
            {
                var generation = _generation;
                try
                {
                    var saved = await write(snapshot());
                    acceptMetadata(saved);
                    _savedGeneration = generation;
                    Error = null;
                    changed();
                }
                catch (Exception ex)
                {
                    Error = ex.Message;
                    changed();
                    return false;
                }
            }
            return !HasChanges;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try { _stopped = true; }
        finally { _gate.Release(); }
    }

    public void Resume()
    {
        _stopped = false;
        if (HasChanges) MarkDirty();
    }
}
