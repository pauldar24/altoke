using System.Text.Json;
namespace GRUPAL.Services;

public sealed class TurnstileService(IHttpClientFactory factory, IConfiguration config,
    IWebHostEnvironment environment, ILogger<TurnstileService> logger)
{
    public async Task<bool> VerificarAsync(string? token, string accion)
    {
        var secret = config["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) return environment.IsDevelopment();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048) return false;
        try
        {
            using var http = factory.CreateClient(); http.Timeout = TimeSpan.FromSeconds(10);
            using var response = await http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["secret"] = secret, ["response"] = token }));
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = body.RootElement;
            var hostname = config["Turnstile:Hostname"] ?? Environment.GetEnvironmentVariable("RENDER_EXTERNAL_HOSTNAME");
            return !string.IsNullOrWhiteSpace(hostname) && root.GetProperty("success").GetBoolean()
                && root.TryGetProperty("hostname", out var host) && host.GetString() == hostname
                && root.TryGetProperty("action", out var action) && action.GetString() == accion;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            logger.LogWarning("La verificación antiabuso falló ({Tipo}).", ex.GetType().Name);
            return false;
        }
    }
}
