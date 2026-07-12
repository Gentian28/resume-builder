using System.Collections.ObjectModel;
using FluentAssertions;
using ResumeBuilder.Core.UndoRedo;

namespace ResumeBuilder.Tests;

public class TextEditActionTests
{
    [Fact]
    public void ConsecutiveEditsToSameField_CollapseIntoOneUndoStep()
    {
        var manager = new UndoRedoManager();
        var value = "";

        // Typing "abc" one character at a time.
        foreach (var text in new[] { "a", "ab", "abc" })
        {
            var before = value;
            manager.RecordAction(new TextEditAction("Summary", before, text, v => value = v));
            value = text;
        }

        manager.UndoCount.Should().Be(1, "a burst of typing in one field is a single undo step");

        manager.Undo();
        value.Should().Be("", "undo reverts the whole burst, back to the state before typing began");
    }

    [Fact]
    public void EditsToDifferentFields_DoNotMerge()
    {
        var manager = new UndoRedoManager();
        var summary = "";
        var title = "";

        manager.RecordAction(new TextEditAction("Summary", "", "hello", v => summary = v));
        manager.RecordAction(new TextEditAction("JobTitle", "", "dev", v => title = v));

        manager.UndoCount.Should().Be(2);
    }

    [Fact]
    public void EditsSeparatedByThePauseWindow_DoNotMerge()
    {
        var first = new TextEditAction("Summary", "", "a", _ => { });
        var later = new TextEditAction("Summary", "a", "ab", _ => { });

        // Simulate a pause longer than the merge window.
        typeof(TextEditAction)
            .GetProperty(nameof(TextEditAction.LastEditedAt))!
            .SetValue(first, DateTime.Now - TextEditAction.MergeWindow - TimeSpan.FromSeconds(1));

        first.TryMerge(later).Should().BeFalse();
    }

    [Fact]
    public void UndoOfAdd_RemovesTheItemThatWasAdded_NotAnEqualOne()
    {
        var manager = new UndoRedoManager();
        var items = new ObservableCollection<string> { "duplicate", "keep" };

        // Insert an equal value at the end; undo must remove *that* one, not the first match.
        manager.Execute(CollectionChangeAction<string>.Add(items, "duplicate", index: 2));
        items.Should().Equal("duplicate", "keep", "duplicate");

        manager.Undo();

        items.Should().Equal("duplicate", "keep");
    }
}
