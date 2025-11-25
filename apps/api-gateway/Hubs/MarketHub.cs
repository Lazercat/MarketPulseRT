using Microsoft.AspNetCore.SignalR;

namespace MarketPulseRT.Hubs;

public class MarketHub : Hub
{
    public async Task Subscribe(string symbol)
    {
        var group = symbol.ToUpperInvariant();
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task Unsubscribe(string symbol)
    {
        var group = symbol.ToUpperInvariant();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}
