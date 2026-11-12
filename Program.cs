using GRUPAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore.Diagnostics;
using GRUPAL.Services;
using GRUPAL.Hubs;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Render asigna el puerto vía variable de entorno PORT ────────────
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddScoped<RecomendadorObjetos>();
builder.Services.AddSingleton<CategoriaObjetoService>();
builder.Services.AddSingleton<ModeloCoincidenciasService>();
builder.Services.AddSingleton<ModeloSpamService>();
builder.Services.AddSingleton<ModeloCalidadAvisoService>();
builder.Services.AddScoped<AsistenteAlToke>();
builder.Services.AddScoped<AlgoliaObjetosService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ModeracionService>();
builder.Services.AddScoped<CorreoService>();
builder.Services.AddScoped<TurnstileService>();

const string googleExternalCookie = "GoogleExternal";
const string googleScheme = "Google";
builder.Services.AddAuthentication();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddCookie(googleExternalCookie, options =>
        {
            options.Cookie.Name = ".AlToke.Google.External";
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            options.SlidingExpiration = false;
        })
        .AddGoogle(googleScheme, options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = googleExternalCookie;
            options.CallbackPath = "/signin-google";
            options.Scope.Add("email");
            options.ClaimActions.MapJsonKey("verified_email", "verified_email");
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/Cuenta/Ingresar?googleError=1");
                return Task.CompletedTask;
            };
        });
}
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "AlToke - API de objetos", Version = "v1",
    Description = "Lectura pública; POST, PUT y DELETE requieren sesión y token antifalsificación."
}));

// ── Base de datos SQLite (ruta adaptada a producción) ──────────────
var dbPath = builder.Environment.IsProduction()
    ? "/app/data/Altoke.db"
    : "Altoke.db";

if (builder.Environment.IsProduction())
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// ── Paso 4: Memoria distribuida con Redis ────────────────────────────
var redisConnection = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrEmpty(redisConnection))
{
    // Producción: usar Redis como caché distribuido
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "AlToke_";
    });
}
else
{
    // Desarrollo / Render sin Redis: caché en memoria
    builder.Services.AddDistributedMemoryCache();
}

// ── Paso 4: Sesiones ──────────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".AlToke.Session";
});

var app = builder.Build();
// Render termina HTTPS en su proxy; procesa X-Forwarded-Proto antes de auth y redirecciones.
app.UseForwardedHeaders();

var renderHostname = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_HOSTNAME");
if (!string.IsNullOrWhiteSpace(renderHostname))
{
    app.Use(async (context, next) =>
    {
        if (string.Equals(context.Request.Host.Host, renderHostname, StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Scheme = "https";
        }
        await next(context);
    });
}

// ── Paso 6: Aplicar migraciones automáticamente al iniciar y Seed Data
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("✅ Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error al aplicar migraciones automáticas.");
    }

    // Cargar ejemplos al habilitarlo, también en producción. El seeder solo actúa
    // cuando las tablas de usuarios y objetos están vacías; nunca borra datos existentes.
    if (app.Configuration.GetValue<bool>("SeedDemoData"))
        await DbSeeder.SeedAsync(db);
}

// Preparar el clasificador antes de aceptar peticiones de publicación.
await app.Services.GetRequiredService<CategoriaObjetoService>().PredecirAsync("audífonos");

app.Services.GetRequiredService<ModeloCoincidenciasService>().Preparar();
app.Services.GetRequiredService<ModeloSpamService>().Preparar();
app.Services.GetRequiredService<ModeloCalidadAvisoService>().Preparar();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Solo redirigir a HTTPS en desarrollo; en Render lo maneja el proxy
    app.UseHttpsRedirection();
}

// ── Paso 6: Archivos estáticos (CSS, JS, imágenes) ────────────────
// UseStaticFiles() garantiza que wwwroot se sirva correctamente en Docker
app.UseStaticFiles();
app.UseRouting();

// El middleware de sesión debe ir ANTES de UseAuthorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapHub<ObjetosHub>("/hubs/objetos");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
