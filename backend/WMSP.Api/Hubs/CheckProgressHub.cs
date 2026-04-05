using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WMSP.Api.Hubs;

[Authorize]
public class CheckProgressHub : Hub
{
    /// <summary>客户端加入某个计划的实时推送组</summary>
    public async Task JoinPlanGroup(long planId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"plan-{planId}");
    }

    /// <summary>客户端离开计划的实时推送组</summary>
    public async Task LeavePlanGroup(long planId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"plan-{planId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
