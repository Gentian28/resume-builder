using AwesomeAssertions;
using ResumeBuilder.Core.Editing;

namespace ResumeBuilder.Tests;

/// <summary>
/// The tracker exists for one race: an edit that lands between a save reading the editor and the
/// save clearing the flag. These pin that the flag is cleared only for the edits the save covered.
/// </summary>
public class DirtyTrackerTests
{
    [Fact]
    public void CompleteSave_WithNoEditsInFlight_ClearsTheFlag()
    {
        var tracker = new DirtyTracker();
        tracker.MarkDirty();

        var token = tracker.BeginSave();
        var settled = tracker.CompleteSave(token);

        settled.Should().BeTrue();
        tracker.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void CompleteSave_AfterAnEditDuringTheSave_StaysDirty()
    {
        var tracker = new DirtyTracker();
        tracker.MarkDirty();

        var token = tracker.BeginSave();
        tracker.MarkDirty(); // typed while the database write was awaited
        var settled = tracker.CompleteSave(token);

        settled.Should().BeFalse();
        tracker.IsDirty.Should().BeTrue("the edit made during the save was not in what was written");
    }

    [Fact]
    public void ANewerSave_SettlesTheEditTheOlderOneMissed()
    {
        var tracker = new DirtyTracker();
        tracker.MarkDirty();
        var first = tracker.BeginSave();
        tracker.MarkDirty();
        tracker.CompleteSave(first).Should().BeFalse();

        var second = tracker.BeginSave();
        tracker.CompleteSave(second).Should().BeTrue();
        tracker.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Reset_MakesAnInFlightSaveStale()
    {
        var tracker = new DirtyTracker();
        tracker.MarkDirty();
        var token = tracker.BeginSave();

        tracker.Reset(); // a different document was loaded while the save ran

        tracker.IsDirty.Should().BeFalse();
        tracker.CompleteSave(token).Should().BeFalse("a save started against the previous document must not settle anything");
        tracker.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void AFreshTracker_IsClean()
    {
        new DirtyTracker().IsDirty.Should().BeFalse();
    }
}
