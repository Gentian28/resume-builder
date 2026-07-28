using AwesomeAssertions;
using ResumeBuilder.Core.UndoRedo;
using System.Collections.ObjectModel;

namespace ResumeBuilder.Tests;

public class UndoRedoManagerTests
{
    [Fact]
    public void Execute_AddsActionToUndoStack()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = "initial";
        var action = new PropertyChangeAction<string>(
            v => value = v ?? "",
            "initial",
            "changed",
            "Change value");

        // Act
        manager.Execute(action);

        // Assert
        manager.CanUndo.Should().BeTrue();
        manager.UndoCount.Should().Be(1);
        value.Should().Be("changed");
    }

    [Fact]
    public void Undo_ReversesLastAction()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = "initial";
        var action = new PropertyChangeAction<string>(
            v => value = v ?? "",
            "initial",
            "changed",
            "Change value");
        manager.Execute(action);

        // Act
        manager.Undo();

        // Assert
        value.Should().Be("initial");
        manager.CanUndo.Should().BeFalse();
        manager.CanRedo.Should().BeTrue();
    }

    [Fact]
    public void Redo_ReappliesUndoneAction()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = "initial";
        var action = new PropertyChangeAction<string>(
            v => value = v ?? "",
            "initial",
            "changed",
            "Change value");
        manager.Execute(action);
        manager.Undo();

        // Act
        manager.Redo();

        // Assert
        value.Should().Be("changed");
        manager.CanUndo.Should().BeTrue();
        manager.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Execute_ClearsRedoStack()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = "initial";
        var action1 = new PropertyChangeAction<string>(
            v => value = v ?? "",
            "initial",
            "first",
            "First change");
        var action2 = new PropertyChangeAction<string>(
            v => value = v ?? "",
            "first",
            "second",
            "Second change");

        manager.Execute(action1);
        manager.Undo();
        manager.CanRedo.Should().BeTrue();

        // Act
        manager.Execute(action2);

        // Assert
        manager.CanRedo.Should().BeFalse();
        manager.RedoCount.Should().Be(0);
    }

    [Fact]
    public void Clear_RemovesAllHistory()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = "initial";
        manager.Execute(new PropertyChangeAction<string>(
            v => value = v ?? "",
            "initial",
            "changed",
            "Change value"));

        // Act
        manager.Clear();

        // Assert
        manager.CanUndo.Should().BeFalse();
        manager.CanRedo.Should().BeFalse();
        manager.UndoCount.Should().Be(0);
        manager.RedoCount.Should().Be(0);
    }

    [Fact]
    public void NextUndoDescription_ReturnsCorrectDescription()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = "initial";
        manager.Execute(new PropertyChangeAction<string>(
            v => value = v ?? "",
            "initial",
            "changed",
            "Change name"));

        // Act & Assert
        manager.NextUndoDescription.Should().Be("Change name");
    }

    [Fact]
    public void NextRedoDescription_ReturnsCorrectDescription()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = "initial";
        manager.Execute(new PropertyChangeAction<string>(
            v => value = v ?? "",
            "initial",
            "changed",
            "Change name"));
        manager.Undo();

        // Act & Assert
        manager.NextRedoDescription.Should().Be("Change name");
    }

    [Fact]
    public void RecordAction_AddsToUndoStack_WithoutExecuting()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var executed = false;
        var action = new TestAction(() => executed = true, () => { }, "Test");

        // Act
        manager.RecordAction(action);

        // Assert
        executed.Should().BeFalse();
        manager.CanUndo.Should().BeTrue();
    }

    [Fact]
    public void MaxHistorySize_TrimsOldActions()
    {
        // Arrange
        var manager = new UndoRedoManager(maxHistorySize: 3);
        var value = 0;

        // Act - Add 5 actions
        for (int i = 1; i <= 5; i++)
        {
            var oldValue = value;
            var newValue = i;
            manager.Execute(new PropertyChangeAction<int>(
                v => value = v,
                oldValue,
                newValue,
                $"Set to {i}"));
        }

        // Assert - Should only keep last 3
        manager.UndoCount.Should().Be(3);
    }

    [Fact]
    public void MultipleUndos_WorkCorrectly()
    {
        // Arrange
        var manager = new UndoRedoManager();
        var value = 0;

        manager.Execute(new PropertyChangeAction<int>(v => value = v, 0, 1, "Set 1"));
        manager.Execute(new PropertyChangeAction<int>(v => value = v, 1, 2, "Set 2"));
        manager.Execute(new PropertyChangeAction<int>(v => value = v, 2, 3, "Set 3"));

        // Act
        manager.Undo(); // 3 -> 2
        manager.Undo(); // 2 -> 1
        manager.Undo(); // 1 -> 0

        // Assert
        value.Should().Be(0);
        manager.CanUndo.Should().BeFalse();
        manager.RedoCount.Should().Be(3);
    }

    [Fact]
    public void ActionExecuted_EventFired()
    {
        // Arrange
        var manager = new UndoRedoManager();
        IUndoableAction? firedAction = null;
        manager.ActionExecuted += a => firedAction = a;
        var value = "test";
        var action = new PropertyChangeAction<string>(v => value = v ?? "", "test", "new", "Change");

        // Act
        manager.Execute(action);

        // Assert
        firedAction.Should().Be(action);
    }

    [Fact]
    public void ActionUndone_EventFired()
    {
        // Arrange
        var manager = new UndoRedoManager();
        IUndoableAction? firedAction = null;
        manager.ActionUndone += a => firedAction = a;
        var value = "test";
        var action = new PropertyChangeAction<string>(v => value = v ?? "", "test", "new", "Change");
        manager.Execute(action);

        // Act
        manager.Undo();

        // Assert
        firedAction.Should().Be(action);
    }

    private class TestAction : IUndoableAction
    {
        private readonly Action _execute;
        private readonly Action _undo;

        public TestAction(Action execute, Action undo, string description)
        {
            _execute = execute;
            _undo = undo;
            Description = description;
        }

        public string Description { get; }
        public DateTime CreatedAt => DateTime.Now;
        public void Execute() => _execute();
        public void Undo() => _undo();
    }
}

public class CollectionChangeActionTests
{
    [Fact]
    public void Add_ExecuteAddsItem()
    {
        // Arrange
        var collection = new ObservableCollection<string>();
        var action = CollectionChangeAction<string>.Add(collection, "item1");

        // Act
        action.Execute();

        // Assert
        collection.Should().Contain("item1");
    }

    [Fact]
    public void Add_UndoRemovesItem()
    {
        // Arrange
        var collection = new ObservableCollection<string>();
        var action = CollectionChangeAction<string>.Add(collection, "item1");
        action.Execute();

        // Act
        action.Undo();

        // Assert
        collection.Should().BeEmpty();
    }

    [Fact]
    public void Remove_ExecuteRemovesItem()
    {
        // Arrange
        var collection = new ObservableCollection<string> { "item1", "item2" };
        var action = CollectionChangeAction<string>.Remove(collection, "item1", 0);

        // Act
        action.Execute();

        // Assert
        collection.Should().NotContain("item1");
        collection.Should().HaveCount(1);
    }

    [Fact]
    public void Remove_UndoRestoresItem()
    {
        // Arrange
        var collection = new ObservableCollection<string> { "item1", "item2" };
        var action = CollectionChangeAction<string>.Remove(collection, "item1", 0);
        action.Execute();

        // Act
        action.Undo();

        // Assert
        collection.Should().Contain("item1");
        collection[0].Should().Be("item1");
    }

    [Fact]
    public void Move_ExecuteMovesItem()
    {
        // Arrange
        var collection = new ObservableCollection<string> { "a", "b", "c" };
        var action = CollectionChangeAction<string>.Move(collection, 0, 2);

        // Act
        action.Execute();

        // Assert
        collection[2].Should().Be("a");
        collection[0].Should().Be("b");
    }

    [Fact]
    public void Move_UndoRestoresPosition()
    {
        // Arrange
        var collection = new ObservableCollection<string> { "a", "b", "c" };
        var action = CollectionChangeAction<string>.Move(collection, 0, 2);
        action.Execute();

        // Act
        action.Undo();

        // Assert
        collection[0].Should().Be("a");
        collection[1].Should().Be("b");
        collection[2].Should().Be("c");
    }

    [Fact]
    public void Clear_ExecuteClearsCollection()
    {
        // Arrange
        var collection = new ObservableCollection<string> { "a", "b", "c" };
        var action = CollectionChangeAction<string>.Clear(collection);

        // Act
        action.Execute();

        // Assert
        collection.Should().BeEmpty();
    }

    [Fact]
    public void Clear_UndoRestoresItems()
    {
        // Arrange
        var collection = new ObservableCollection<string> { "a", "b", "c" };
        var action = CollectionChangeAction<string>.Clear(collection);
        action.Execute();

        // Act
        action.Undo();

        // Assert
        collection.Should().HaveCount(3);
        collection.Should().ContainInOrder("a", "b", "c");
    }
}

public class CompositeActionTests
{
    [Fact]
    public void Execute_ExecutesAllActions()
    {
        // Arrange
        var values = new List<int>();
        var composite = new CompositeAction("Multiple adds",
            new TestAction(() => values.Add(1), () => values.Remove(1)),
            new TestAction(() => values.Add(2), () => values.Remove(2)),
            new TestAction(() => values.Add(3), () => values.Remove(3)));

        // Act
        composite.Execute();

        // Assert
        values.Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void Undo_UndoesInReverseOrder()
    {
        // Arrange
        var undoOrder = new List<int>();
        var composite = new CompositeAction("Multiple adds",
            new TestAction(() => { }, () => undoOrder.Add(1)),
            new TestAction(() => { }, () => undoOrder.Add(2)),
            new TestAction(() => { }, () => undoOrder.Add(3)));
        composite.Execute();

        // Act
        composite.Undo();

        // Assert
        undoOrder.Should().ContainInOrder(3, 2, 1);
    }

    private class TestAction : IUndoableAction
    {
        private readonly Action _execute;
        private readonly Action _undo;

        public TestAction(Action execute, Action undo)
        {
            _execute = execute;
            _undo = undo;
        }

        public string Description => "Test";
        public DateTime CreatedAt => DateTime.Now;
        public void Execute() => _execute();
        public void Undo() => _undo();
    }
}
