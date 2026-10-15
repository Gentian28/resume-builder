using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResumeBuilder.App.Services;

/// <summary>Only fixed codes are retained; the displayed error text never enters diagnostics.</summary>
public sealed class DiagnosticDialogs(IUserDialogs inner) : IUserDialogs
{
    private static readonly Queue<string> RecentCodes = new();
    public static IReadOnlyList<string> Codes { get { lock (RecentCodes) return RecentCodes.ToArray(); } }
    public Task ShowErrorAsync(string title, string message)
    {
        lock (RecentCodes)
        {
            if (RecentCodes.Count == 10) RecentCodes.Dequeue();
            RecentCodes.Enqueue(title switch { "Save failed" => "SAVE_FAILED", "Import failed" => "IMPORT_FAILED", "Export failed" => "EXPORT_FAILED", "Sync failed" => "SYNC_FAILED", _ => "OPERATION_FAILED" });
        }
        return inner.ShowErrorAsync(title, message);
    }
    public Task<bool> ConfirmAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel") => inner.ConfirmAsync(title, message, confirmText, cancelText);
    public Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string name) => inner.ConfirmUnsavedChangesAsync(name);
    public Task<string?> PromptAsync(string title, string message, string initialValue = "") => inner.PromptAsync(title, message, initialValue);
}
