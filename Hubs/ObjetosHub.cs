using Microsoft.AspNetCore.SignalR;

namespace GRUPAL.Hubs;

// El hub solo publica avisos visibles para todos; los clientes no pueden invocar acciones.
public sealed class ObjetosHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var usuario = Context.GetHttpContext()?.Session.GetInt32("UsuarioId");
        if (usuario.HasValue) await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario-{usuario}");
        await base.OnConnectedAsync();
    }
}
