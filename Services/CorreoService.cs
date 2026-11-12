using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace GRUPAL.Services;

public sealed class CorreoService(IHttpClientFactory factory, IConfiguration config, ILogger<CorreoService> logger)
{
    public async Task<bool> EnviarAsync(string correo, string asunto, string texto, string clave)
    {
        if (string.IsNullOrWhiteSpace(config["Resend:ApiKey"]) || string.IsNullOrWhiteSpace(config["Resend:From"])) return false;
        try
        {
            using var http = factory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config["Resend:ApiKey"]);
            solicitud.Headers.Add("Idempotency-Key", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clave))));
            solicitud.Content = JsonContent.Create(new { from = config["Resend:From"], to = new[] { correo }, subject = asunto, text = texto });
            using var respuesta = await http.SendAsync(solicitud);
            return respuesta.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("El correo no pudo enviarse ({Tipo}).", ex.GetType().Name);
            return false;
        }
    }
}
