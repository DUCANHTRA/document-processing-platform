using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DocumentProcessing.Api.Hubs;

[Authorize]
public class ProcessingHub : Hub
{
}