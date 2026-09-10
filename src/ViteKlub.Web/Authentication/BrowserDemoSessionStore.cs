using Microsoft.JSInterop;

namespace ViteKlub.Web.Authentication;

public sealed class BrowserDemoSessionStore(IJSRuntime jsRuntime) : IDemoSessionStore
{
    private const string StorageKey = "viteklub.demo.current-user";

    public Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, StorageKey).AsTask();

    public Task SetUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, StorageKey, userId).AsTask();

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, StorageKey).AsTask();
}
