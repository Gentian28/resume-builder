using System.Collections.ObjectModel;

namespace ResumeBuilder.Core.UndoRedo;

/// <summary>
/// Type of collection change operation.
/// </summary>
public enum CollectionChangeType
{
    Add,
    Remove,
    Move,
    Replace,
    Clear
}

/// <summary>
/// Represents a collection modification that can be undone.
/// </summary>
public class CollectionChangeAction<T> : IUndoableAction
{
    private readonly ObservableCollection<T> _collection;
    private readonly CollectionChangeType _changeType;
    private readonly T? _item;
    private readonly int _oldIndex;
    private readonly int _newIndex;
    private readonly List<T>? _clearedItems;
    private readonly Action? _onChanged;

    public string Description { get; }
    public DateTime CreatedAt { get; }

    private CollectionChangeAction(
        ObservableCollection<T> collection,
        CollectionChangeType changeType,
        T? item,
        int oldIndex,
        int newIndex,
        List<T>? clearedItems,
        string description,
        Action? onChanged)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        _changeType = changeType;
        _item = item;
        _oldIndex = oldIndex;
        _newIndex = newIndex;
        _clearedItems = clearedItems;
        _onChanged = onChanged;
        Description = description;
        CreatedAt = DateTime.Now;
    }

    /// <summary>
    /// Creates an action for adding an item to a collection.
    /// </summary>
    public static CollectionChangeAction<T> Add(
        ObservableCollection<T> collection,
        T item,
        int index = -1,
        string? itemDescription = null,
        Action? onChanged = null)
    {
        var actualIndex = index >= 0 ? index : collection.Count;
        var desc = itemDescription ?? typeof(T).Name;
        return new CollectionChangeAction<T>(
            collection,
            CollectionChangeType.Add,
            item,
            actualIndex,
            actualIndex,
            null,
            $"Add {desc}",
            onChanged);
    }

    /// <summary>
    /// Creates an action for removing an item from a collection.
    /// </summary>
    public static CollectionChangeAction<T> Remove(
        ObservableCollection<T> collection,
        T item,
        int index,
        string? itemDescription = null,
        Action? onChanged = null)
    {
        var desc = itemDescription ?? typeof(T).Name;
        return new CollectionChangeAction<T>(
            collection,
            CollectionChangeType.Remove,
            item,
            index,
            index,
            null,
            $"Remove {desc}",
            onChanged);
    }

    /// <summary>
    /// Creates an action for moving an item within a collection.
    /// </summary>
    public static CollectionChangeAction<T> Move(
        ObservableCollection<T> collection,
        int oldIndex,
        int newIndex,
        string? itemDescription = null,
        Action? onChanged = null)
    {
        var item = collection[oldIndex];
        var desc = itemDescription ?? typeof(T).Name;
        var direction = newIndex > oldIndex ? "down" : "up";
        return new CollectionChangeAction<T>(
            collection,
            CollectionChangeType.Move,
            item,
            oldIndex,
            newIndex,
            null,
            $"Move {desc} {direction}",
            onChanged);
    }

    /// <summary>
    /// Creates an action for clearing a collection.
    /// </summary>
    public static CollectionChangeAction<T> Clear(
        ObservableCollection<T> collection,
        string? collectionDescription = null,
        Action? onChanged = null)
    {
        var items = collection.ToList();
        var desc = collectionDescription ?? $"{typeof(T).Name} list";
        return new CollectionChangeAction<T>(
            collection,
            CollectionChangeType.Clear,
            default,
            0,
            0,
            items,
            $"Clear {desc}",
            onChanged);
    }

    public void Execute()
    {
        switch (_changeType)
        {
            case CollectionChangeType.Add:
                if (_oldIndex >= _collection.Count)
                    _collection.Add(_item!);
                else
                    _collection.Insert(_oldIndex, _item!);
                break;

            case CollectionChangeType.Remove:
                _collection.RemoveAt(_oldIndex);
                break;

            case CollectionChangeType.Move:
                _collection.Move(_oldIndex, _newIndex);
                break;

            case CollectionChangeType.Clear:
                _collection.Clear();
                break;
        }

        _onChanged?.Invoke();
    }

    public void Undo()
    {
        switch (_changeType)
        {
            case CollectionChangeType.Add:
                // Remove by position, not by value: two equal items (e.g. two blank skills) would
                // otherwise make Remove(item) delete the wrong one.
                if (_oldIndex >= 0 && _oldIndex < _collection.Count)
                    _collection.RemoveAt(_oldIndex);
                else
                    _collection.Remove(_item!);
                break;

            case CollectionChangeType.Remove:
                if (_oldIndex >= _collection.Count)
                    _collection.Add(_item!);
                else
                    _collection.Insert(_oldIndex, _item!);
                break;

            case CollectionChangeType.Move:
                _collection.Move(_newIndex, _oldIndex);
                break;

            case CollectionChangeType.Clear:
                if (_clearedItems != null)
                {
                    foreach (var item in _clearedItems)
                    {
                        _collection.Add(item);
                    }
                }
                break;
        }

        _onChanged?.Invoke();
    }
}
