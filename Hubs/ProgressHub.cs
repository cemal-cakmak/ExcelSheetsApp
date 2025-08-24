using Microsoft.AspNetCore.SignalR;

namespace ExcelSheetsApp.Hubs;

public class ProgressHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task UpdateProgress(string groupName, int progress, string message)
    {
        await Clients.Group(groupName).SendAsync("ProgressUpdated", progress, message);
    }

    public async Task SendNotification(string groupName, string type, string message)
    {
        await Clients.Group(groupName).SendAsync("NotificationReceived", type, message);
    }

    public async Task UpdateFileUploadProgress(string groupName, int progress, string fileName)
    {
        await Clients.Group(groupName).SendAsync("FileUploadProgress", progress, fileName);
    }

    public async Task UpdateSeleniumProgress(string groupName, int progress, string status, List<string> logs)
    {
        await Clients.Group(groupName).SendAsync("SeleniumProgress", progress, status, logs);
    }

    public async Task UpdateSystemStatus(string groupName, object status)
    {
        await Clients.Group(groupName).SendAsync("SystemStatusUpdated", status);
    }
}
