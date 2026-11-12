using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Models;
using Microsoft.AspNetCore.Identity;

namespace GRUPAL.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Nunca eliminar datos existentes al iniciar el servidor.
        if (await context.Usuarios.AnyAsync() || await context.ObjetosPerdidos.AnyAsync())
        {
            await ActualizarMuestrasAsync(context);
            return;
        }

        var users = new List<Usuario>
        {
            new Usuario { Nombre = "Carlos Mendoza", Correo = "carlos.mendoza@usmp.pe", Contraseña = "Carlos123!", PublicacionesActivas = 3, HistoriasResueltas = 1, BuenasAcciones = 2 },
            new Usuario { Nombre = "María Fernández", Correo = "maria.fernandez@usmp.pe", Contraseña = "Maria123!", PublicacionesActivas = 2, HistoriasResueltas = 2, BuenasAcciones = 4 },
            new Usuario { Nombre = "José Quispe", Correo = "jose.quispe@usmp.pe", Contraseña = "Jose123!", PublicacionesActivas = 1, HistoriasResueltas = 0, BuenasAcciones = 1 },
            new Usuario { Nombre = "Ana Lucía Torres", Correo = "analucia.torres@usmp.pe", Contraseña = "Ana123!", PublicacionesActivas = 4, HistoriasResueltas = 3, BuenasAcciones = 5 },
            new Usuario { Nombre = "Diego Ramírez", Correo = "diego.ramirez@usmp.pe", Contraseña = "Diego123!", PublicacionesActivas = 2, HistoriasResueltas = 1, BuenasAcciones = 2 },
            new Usuario { Nombre = "Valentina Chávez", Correo = "valentina.chavez@usmp.pe", Contraseña = "Valentina123!", PublicacionesActivas = 1, HistoriasResueltas = 1, BuenasAcciones = 3 },
            new Usuario { Nombre = "Andrés Paredes", Correo = "andres.paredes@usmp.pe", Contraseña = "Andres123!", PublicacionesActivas = 3, HistoriasResueltas = 2, BuenasAcciones = 3 },
            new Usuario { Nombre = "Camila Rojas", Correo = "camila.rojas@usmp.pe", Contraseña = "Camila123!", PublicacionesActivas = 2, HistoriasResueltas = 0, BuenasAcciones = 1 },
            new Usuario { Nombre = "Luis García", Correo = "luis.garcia@usmp.pe", Contraseña = "Luis123!", PublicacionesActivas = 1, HistoriasResueltas = 1, BuenasAcciones = 2 },
            new Usuario { Nombre = "Sofía Huamán", Correo = "sofia.huaman@usmp.pe", Contraseña = "Sofia123!", PublicacionesActivas = 2, HistoriasResueltas = 2, BuenasAcciones = 4 },
            new Usuario { Nombre = "Fernando Castillo", Correo = "fernando.castillo@usmp.pe", Contraseña = "Fernando123!", PublicacionesActivas = 1, HistoriasResueltas = 0, BuenasAcciones = 0 },
            new Usuario { Nombre = "Isabella Vargas", Correo = "isabella.vargas@usmp.pe", Contraseña = "Isabella123!", PublicacionesActivas = 3, HistoriasResueltas = 1, BuenasAcciones = 2 }
        };

        var hasher = new PasswordHasher<Usuario>();
        foreach (var user in users) user.Contraseña = hasher.HashPassword(user, user.Contraseña);
        await context.Usuarios.AddRangeAsync(users);
        await context.SaveChangesAsync();

        var objetos = CrearMuestras(users.Select(u => u.Id).ToArray());

        await context.ObjetosPerdidos.AddRangeAsync(objetos);
        await context.SaveChangesAsync();
    }
    // Sólo se sustituyen las muestras originales con imagen aleatoria y cuentas del seeder.
    // Un aviso con solicitudes, favoritos o notificaciones se conserva con su historial.
    private static async Task ActualizarMuestrasAsync(ApplicationDbContext context)
    {
        var correos = new[] { "carlos.mendoza@usmp.pe", "maria.fernandez@usmp.pe", "jose.quispe@usmp.pe", "analucia.torres@usmp.pe", "diego.ramirez@usmp.pe", "valentina.chavez@usmp.pe", "andres.paredes@usmp.pe", "camila.rojas@usmp.pe", "luis.garcia@usmp.pe", "sofia.huaman@usmp.pe", "fernando.castillo@usmp.pe", "isabella.vargas@usmp.pe" };
        var ids = await context.Usuarios.Where(u => correos.Contains(u.Correo)).Select(u => u.Id).ToArrayAsync();
        // Actualizar sólo el catálogo ilustrado anterior, sin modificar estado, fecha ni historial.
        if (ids.Length > 0)
        {
            var ilustradas = await context.ObjetosPerdidos.Where(o => ids.Contains(o.UsuarioId)
                && o.FotoUrl != null && o.FotoUrl.StartsWith("/images/demo/") && o.FotoUrl.EndsWith(".svg")).ToListAsync();
            var fotos = CrearMuestras(ids);
            foreach (var aviso in ilustradas)
            {
                var nueva = fotos.FirstOrDefault(o => o.FotoUrl == aviso.FotoUrl!.Replace(".svg", ".jpg"));
                if (nueva == null) continue;
                aviso.FotoUrl = nueva.FotoUrl;
                aviso.Título = nueva.Título;
                aviso.Descripcion = nueva.Descripcion;
            }
            await context.SaveChangesAsync();
        }
        var antiguas = await context.ObjetosPerdidos.Where(o => ids.Contains(o.UsuarioId)
            && o.FotoUrl != null && o.FotoUrl.StartsWith("https://picsum.photos/seed/")
            && !context.SolicitudesRecuperacion.Any(r => r.ObjetoPerdidoId == o.Id)
            && !context.Favoritos.Any(f => f.ObjetoPerdidoId == o.Id)
            && !context.Notificaciones.Any(n => n.ObjetoPerdidoId == o.Id)).ToListAsync();
        if (antiguas.Count == 0 || ids.Length == 0) return;
        await using var transaccion = await context.Database.BeginTransactionAsync();
        var yaActualizadas = await context.ObjetosPerdidos.AnyAsync(o => ids.Contains(o.UsuarioId)
            && o.FotoUrl != null && o.FotoUrl.StartsWith("/images/demo/"));
        context.ObjetosPerdidos.RemoveRange(antiguas);
        if (!yaActualizadas) await context.ObjetosPerdidos.AddRangeAsync(CrearMuestras(ids));
        await context.SaveChangesAsync();
        await transaccion.CommitAsync();
    }

    private static List<ObjetoPerdido> CrearMuestras(int[] usuarios)
    {
        var hoy = DateTime.UtcNow.AddHours(-5).Date;
        return new List<ObjetoPerdido>
        {
            new() { Título = "Audífonos inalámbricos negros", Categoría = "Electrónicos", Descripcion = "Perdí mis audífonos de diadema negros al salir de la biblioteca. Tienen almohadillas oscuras y una pequeña marca de uso en el lado derecho. Fotografía referencial para esta publicación de prueba.", Ubicacion = "Biblioteca central", FotoUrl = "/images/demo/audifonos.jpg", TipoPublicacion = TipoPublicacion.Dueno },
            new() { Título = "Mochila negra y marrón con bolsillo frontal", Categoría = "Accesorios", Descripcion = "Encontré una mochila oscura con bolsillo frontal y asas de cuero en una banca. La entregué a seguridad; quien sea su dueño puede describir su contenido por chat privado. Fotografía referencial para esta publicación de prueba.", Ubicacion = "Patio principal", FotoUrl = "/images/demo/mochila.jpg", TipoPublicacion = TipoPublicacion.Hallazgo },
            new() { Título = "Laptop plateada de 14 pulgadas", Categoría = "Electrónicos", Descripcion = "Olvidé mi laptop plateada de 14 pulgadas en una mesa de estudio. Tiene una pequeña marca en la esquina de la tapa y estaba dentro de una funda gris. Fotografía referencial para esta publicación de prueba.", Ubicacion = "Sala de estudio", FotoUrl = "/images/demo/laptop.jpg", TipoPublicacion = TipoPublicacion.Dueno },
            new() { Título = "Carnet universitario", Categoría = "Documentos", Descripcion = "Encontré un carnet universitario cerca de la cafetería y lo dejé en seguridad. Por privacidad no publico el nombre ni el código; el dueño puede identificarse por chat privado. Fotografía de muestra de un carnet sin datos personales.", Ubicacion = "Cafetería", FotoUrl = "/images/demo/carnet.jpg", TipoPublicacion = TipoPublicacion.Hallazgo },
            new() { Título = "Llaves de auto con control negro", Categoría = "Llaves", Descripcion = "Perdí mis llaves de auto con control negro y varias llaves y una argolla metálica. La argolla tiene una pequeña marca roja. Posiblemente se quedaron cerca del estacionamiento. Fotografía referencial para esta publicación de prueba.", Ubicacion = "Estacionamiento", FotoUrl = "/images/demo/llaves.jpg", TipoPublicacion = TipoPublicacion.Dueno },
            new() { Título = "Cargador blanco USB-C", Categoría = "Electrónicos", Descripcion = "Encontré un cargador blanco USB con su cable conectado junto a una mesa. Está en seguridad; el dueño puede indicar la potencia y alguna característica para reconocerlo. Fotografía referencial para esta publicación de prueba.", Ubicacion = "Aula de informática", FotoUrl = "/images/demo/cargador.jpg", TipoPublicacion = TipoPublicacion.Hallazgo }
        }.Select((o, i) => { o.UsuarioId = usuarios[i % usuarios.Length]; o.Fecha = hoy.AddDays(-i); o.Estado = EstadoObjeto.Perdido; return o; }).ToList();
    }

}
