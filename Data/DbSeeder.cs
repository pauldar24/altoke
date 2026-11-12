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
        if (await context.Usuarios.AnyAsync() || await context.ObjetosPerdidos.AnyAsync()) return;

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

        var objetos = new List<ObjetoPerdido>
        {
            // Electrónicos (6) - 4 Perdido, 2 Encontrado
            new ObjetoPerdido { Título = "iPhone 14 Pro negro", Categoría = "Electrónicos", Descripcion = "Lo dejé en la mesa de la cafetería del pabellón B. Tiene un case negro de silicona y la pantalla está un poco rajada en la esquina superior derecha.", Fecha = new DateTime(2026, 8, 14), Ubicacion = "Cafetería pabellón B", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/iphone/600/400", UsuarioId = users[0].Id },
            new ObjetoPerdido { Título = "AirPods Pro en estuche blanco", Categoría = "Electrónicos", Descripcion = "Se me cayeron saliendo de la biblioteca central. El estuche tiene grabadas las iniciales 'MF'. Ofrezco recompensa.", Fecha = new DateTime(2026, 8, 16), Ubicacion = "Biblioteca central USMP", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/airpods/600/400", UsuarioId = users[1].Id },
            new ObjetoPerdido { Título = "Cargador MacBook USB-C", Categoría = "Electrónicos", Descripcion = "Encontré este cargador original de Apple enchufado en el aula 304 del pabellón A. Lo dejé con la secretaria de la facultad.", Fecha = new DateTime(2026, 8, 20), Ubicacion = "Aula 304 Pabellón A", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/maccharger/600/400", UsuarioId = users[2].Id },
            new ObjetoPerdido { Título = "Tablet Samsung Galaxy Tab A9", Categoría = "Electrónicos", Descripcion = "Olvidé mi tablet en la sala de estudios del segundo piso. Tiene una funda tipo libro color azul marino.", Fecha = new DateTime(2026, 8, 25), Ubicacion = "Sala de estudios 2do piso", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/tablet/600/400", UsuarioId = users[3].Id },
            new ObjetoPerdido { Título = "Mouse inalámbrico Logitech", Categoría = "Electrónicos", Descripcion = "Mouse color gris oscuro. Lo dejaron en una de las computadoras del laboratorio de sistemas L-102.", Fecha = new DateTime(2026, 8, 28), Ubicacion = "Laboratorio L-102", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/mouse/600/400", UsuarioId = users[4].Id },
            new ObjetoPerdido { Título = "Memoria USB Kingston 64GB", Categoría = "Electrónicos", Descripcion = "Perdí mi USB rojo metálico con todos mis trabajos de final de ciclo. Por favor, si lo encuentran avísenme, es muy urgente.", Fecha = new DateTime(2026, 9, 1), Ubicacion = "Pasillos Facultad de Derecho", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/usb/600/400", UsuarioId = users[5].Id },

            // Documentos (5) - 3 Perdido, 2 Encontrado
            new ObjetoPerdido { Título = "DNI a nombre de Carlos M.", Categoría = "Documentos", Descripcion = "Se me cayó mi DNI bajando del bus en el paradero de la avenida. Por favor, lo necesito para un trámite mañana.", Fecha = new DateTime(2026, 9, 3), Ubicacion = "Paradero Av. La Fontana", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/dni/600/400", UsuarioId = users[6].Id },
            new ObjetoPerdido { Título = "Carnet universitario USMP 2026", Categoría = "Documentos", Descripcion = "Carnet universitario encontrado cerca al cajero automático del campus. Está a nombre de una estudiante de psicología.", Fecha = new DateTime(2026, 9, 5), Ubicacion = "Cajero automático USMP", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/carnet/600/400", UsuarioId = users[7].Id },
            new ObjetoPerdido { Título = "Licencia de conducir A-I", Categoría = "Documentos", Descripcion = "Encontré un brevete en el estacionamiento de alumnos cerca a la salida principal. Lo entregué a seguridad.", Fecha = new DateTime(2026, 9, 8), Ubicacion = "Estacionamiento campus", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/licencia/600/400", UsuarioId = users[8].Id },
            new ObjetoPerdido { Título = "Pasaporte peruano", Categoría = "Documentos", Descripcion = "Dejé mi pasaporte en la fotocopiadora frente a la puerta 2. Fui a sacar unas copias y al regresar ya no estaba.", Fecha = new DateTime(2026, 9, 10), Ubicacion = "Fotocopiadora frente a puerta 2", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/pasaporte/600/400", UsuarioId = users[9].Id },
            new ObjetoPerdido { Título = "Cuaderno de apuntes Cálculo II", Categoría = "Documentos", Descripcion = "Cuaderno anillado tamaño A4 de tapa azul. Tiene mis apuntes para el parcial, ¡estoy desesperado por recuperarlo!", Fecha = new DateTime(2026, 9, 12), Ubicacion = "Auditorio principal", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/cuaderno/600/400", UsuarioId = users[10].Id },

            // Ropa (4) - 3 Perdido, 1 Encontrado
            new ObjetoPerdido { Título = "Casaca negra The North Face", Categoría = "Ropa", Descripcion = "Dejé mi casaca colgada en la silla del auditorio durante la conferencia de ayer. Es talla M y tiene un parche en la manga.", Fecha = new DateTime(2026, 9, 14), Ubicacion = "Auditorio Facultad de Medicina", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/casaca/600/400", UsuarioId = users[11].Id },
            new ObjetoPerdido { Título = "Polo universitario USMP talla M", Categoría = "Ropa", Descripcion = "Encontré un polo blanco con el logo de la universidad doblado en las bancas de la loza deportiva.", Fecha = new DateTime(2026, 9, 15), Ubicacion = "Loza deportiva", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/polo/600/400", UsuarioId = users[0].Id },
            new ObjetoPerdido { Título = "Bufanda gris de lana", Categoría = "Ropa", Descripcion = "Se me cayó mi chalina camino a la estación del metropolitano. Hace mucho frío y es mi favorita.", Fecha = new DateTime(2026, 9, 16), Ubicacion = "Estación Metropolitano - Javier Prado", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/bufanda/600/400", UsuarioId = users[1].Id },
            new ObjetoPerdido { Título = "Zapatillas Nike Air Force 1 blancas", Categoría = "Ropa", Descripcion = "Olvidé un bolso de gimnasio que contenía mis zapatillas blancas en el baño de hombres del primer piso.", Fecha = new DateTime(2026, 9, 17), Ubicacion = "Baño primer piso", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/zapatillas/600/400", UsuarioId = users[2].Id },

            // Accesorios (5) - 3 Perdido, 2 Encontrado
            new ObjetoPerdido { Título = "Reloj Casio plateado clásico", Categoría = "Accesorios", Descripcion = "Me quité el reloj para lavarme las manos y lo dejé en el lavadero. Es un regalo de mi papá, por favor devuélvanlo.", Fecha = new DateTime(2026, 9, 18), Ubicacion = "Baño de mujeres - Pabellón C", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/reloj/600/400", UsuarioId = users[3].Id },
            new ObjetoPerdido { Título = "Mochila Totto azul marino", Categoría = "Accesorios", Descripcion = "Encontrada mochila azul debajo de una mesa en el patio principal. No se ha abierto por respeto a la privacidad.", Fecha = new DateTime(2026, 9, 19), Ubicacion = "Patio principal", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/mochila/600/400", UsuarioId = users[4].Id },
            new ObjetoPerdido { Título = "Lentes de sol Ray-Ban Aviator", Categoría = "Accesorios", Descripcion = "Perdí mis lentes con marco dorado y lunas oscuras mientras caminaba por el parque frente a la facu.", Fecha = new DateTime(2026, 9, 20), Ubicacion = "Parque frente a la universidad", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/lentes/600/400", UsuarioId = users[5].Id },
            new ObjetoPerdido { Título = "Billetera de cuero marrón", Categoría = "Accesorios", Descripcion = "Billetera Renzo Costa perdida. Solo quiero mis documentos, quédense con el efectivo si quieren, pero devuélvanme los DNI.", Fecha = new DateTime(2026, 9, 21), Ubicacion = "Comedor universitario", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/wallet/600/400", UsuarioId = users[6].Id },
            new ObjetoPerdido { Título = "Audífonos Sony WH-1000XM4", Categoría = "Accesorios", Descripcion = "Audífonos de diadema negros encontrados en una banca del parque central del campus. Están en la oficina de seguridad.", Fecha = new DateTime(2026, 9, 21), Ubicacion = "Parque central campus", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/headset/600/400", UsuarioId = users[7].Id },

            // Llaves (4) - 2 Perdido, 2 Encontrado
            new ObjetoPerdido { Título = "Llavero con 3 llaves y peluche", Categoría = "Llaves", Descripcion = "Llavero de Pikachú con llaves de mi casa. Las dejé en el aula 205 después de clases de programación.", Fecha = new DateTime(2026, 8, 22), Ubicacion = "Aula 205 Pabellón D", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/keys1/600/400", UsuarioId = users[8].Id },
            new ObjetoPerdido { Título = "Llaves de auto Toyota con control", Categoría = "Llaves", Descripcion = "Encontré llaves de auto en el césped cerca a las escaleras de ingreso. Preguntar en vigilancia de la puerta 1.", Fecha = new DateTime(2026, 8, 24), Ubicacion = "Escaleras de ingreso", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/car_keys/600/400", UsuarioId = users[9].Id },
            new ObjetoPerdido { Título = "Llave de casillero #47", Categoría = "Llaves", Descripcion = "Se me perdió la llave metálica pequeña de mi locker. Tiene una cinta roja amarrada.", Fecha = new DateTime(2026, 8, 26), Ubicacion = "Pasadizo de lockers", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/locker_key/600/400", UsuarioId = users[10].Id },
            new ObjetoPerdido { Título = "Juego de llaves con llavero del Perú", Categoría = "Llaves", Descripcion = "Encontradas llaves con un llavero de alpaca. Estaban tiradas en el paradero de los buses de la universidad.", Fecha = new DateTime(2026, 9, 2), Ubicacion = "Paradero buses USMP", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/keys3/600/400", UsuarioId = users[11].Id },

            // Mascotas (2) - 1 Perdido, 1 Encontrado
            new ObjetoPerdido { Título = "Gato atigrado gris — zona Surco", Categoría = "Mascotas", Descripcion = "Mi gatito 'Michi' se escapó de casa cerca a la universidad. Tiene un collar azul con cascabel. Es muy asustadizo.", Fecha = new DateTime(2026, 9, 7), Ubicacion = "Urb. Los Rosales, Surco", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/cat/600/400", UsuarioId = users[0].Id },
            new ObjetoPerdido { Título = "Perro golden retriever con collar rojo", Categoría = "Mascotas", Descripcion = "Perrito muy amigable encontrado deambulando por el Ovalo Higuereta. Lo tengo resguardado en mi casa temporalmente.", Fecha = new DateTime(2026, 9, 11), Ubicacion = "Óvalo Higuereta", TipoPublicacion = TipoPublicacion.Hallazgo, Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/dog/600/400", UsuarioId = users[3].Id },

            // Otros (4) - 4 Perdido
            new ObjetoPerdido { Título = "Paraguas negro automático", Categoría = "Otros", Descripcion = "Olvidé mi paraguas ayer que llovió en el salón 102. Es grande y tiene mango de madera.", Fecha = new DateTime(2026, 9, 13), Ubicacion = "Aula 102", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/umbrella/600/400", UsuarioId = users[5].Id },
            new ObjetoPerdido { Título = "Botella de agua Contigo azul", Categoría = "Otros", Descripcion = "Se me quedó mi tomatodo azul en las gradas de la cancha de fútbol. Agradeceré mucho si lo encontraron.", Fecha = new DateTime(2026, 9, 22), Ubicacion = "Cancha de fútbol", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/bottle/600/400", UsuarioId = users[7].Id },
            new ObjetoPerdido { Título = "Estuche de lentes negro", Categoría = "Otros", Descripcion = "Perdí el estuche duro color negro marca Ray-Ban de mis lentes. Lo dejé en mesa de partes.", Fecha = new DateTime(2026, 9, 23), Ubicacion = "Mesa de partes", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/glasses_case/600/400", UsuarioId = users[9].Id },
            new ObjetoPerdido { Título = "Power bank Anker 20000mAh", Categoría = "Otros", Descripcion = "Batería portátil negra pesada. La presté a alguien en la sala de lectura y me fui apurado olvidándola.", Fecha = new DateTime(2026, 9, 24), Ubicacion = "Sala de lectura", Estado = EstadoObjeto.Perdido, FotoUrl = "https://picsum.photos/seed/powerbank/600/400", UsuarioId = users[11].Id }
        };

        await context.ObjetosPerdidos.AddRangeAsync(objetos);
        await context.SaveChangesAsync();
    }
}
