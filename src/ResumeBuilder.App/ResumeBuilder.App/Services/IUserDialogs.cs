using System.Threading.Tasks;

namespace ResumeBuilder.App.Services;

public interface IUserDialogs
{
    Task ShowErrorAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");
    Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string name);
    Task<string?> PromptAsync(string title, string message, string initialValue = "");
}

public sealed class WindowDialogs : IUserDialogs
{
    public static WindowDialogs Instance { get; } = new();
    public Task ShowErrorAsync(string title, string message) => DialogService.ShowErrorAsync(title, message);
    public Task<bool> ConfirmAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel") =>
        DialogService.ConfirmAsync(title, message, confirmText, cancelText);
    public Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync(string name) => DialogService.ConfirmUnsavedChangesAsync(name);
    public Task<string?> PromptAsync(string title, string message, string initialValue = "") => DialogService.PromptAsync(title, message, initialValue);
}
