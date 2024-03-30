using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using TextCopy;
using IClipboard = Avalonia.Input.Platform.IClipboard;

namespace MintyCore.AvaloniaIntegration;

internal class Clipboard : IClipboard
{
    public Task<string?> GetTextAsync()
    {
        return ClipboardService.GetTextAsync();
    }

    public Task SetTextAsync(string? text)
    {
        return ClipboardService.SetTextAsync(text ?? string.Empty, CancellationToken.None);
    }

    public Task ClearAsync()
    {
        return ClipboardService.SetTextAsync(string.Empty, CancellationToken.None);
    }

    public Task SetDataObjectAsync(IDataObject data)
    {
        return Task.CompletedTask;
    }

    public Task<string[]> GetFormatsAsync()
    {
        return Task.FromResult(Array.Empty<string>());
    }

    public Task<object?> GetDataAsync(string format)
    {
        return Task.FromResult<object?>(null);
    }
}