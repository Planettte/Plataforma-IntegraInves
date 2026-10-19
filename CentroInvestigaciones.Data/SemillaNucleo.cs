using CentroInvestigaciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentroInvestigaciones.Data;

public static class SemillaNucleo
{
    public static async Task AplicarAsync(CentroDbContext db, CancellationToken ct = default)
    {
        var areas = new (string Codigo, string Nombre)[]
        {
            ("estudios", "Estudios clínicos"),
            ("udd", "Diseño y desarrollo"),
            ("comite", "Investigaciones")
        };

        foreach (var (codigo, nombre) in areas)
        {
            if (!await db.Areas.AnyAsync(area => area.Codigo == codigo, ct))
            {
                db.Areas.Add(new Area { Id = Guid.NewGuid(), Codigo = codigo, Nombre = nombre });
            }
        }

        foreach (var (codigo, nombre) in PermisosNucleo.Catalogo)
        {
            if (!await db.Permisos.AnyAsync(permiso => permiso.Codigo == codigo, ct))
            {
                db.Permisos.Add(new Permiso { Id = Guid.NewGuid(), Codigo = codigo, Nombre = nombre });
            }
        }

        await db.SaveChangesAsync(ct);

        var rol = await db.Roles.Include(item => item.Permisos)
            .FirstOrDefaultAsync(item => item.Codigo == PermisosNucleo.RolAdministrador, ct);

        if (rol is null)
        {
            rol = new Rol
            {
                Id = Guid.NewGuid(),
                Codigo = PermisosNucleo.RolAdministrador,
                Nombre = "Administrador"
            };
            db.Roles.Add(rol);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            rol.Nombre = "Administrador";
        }

        var permisos = await db.Permisos.ToListAsync(ct);
        foreach (var permiso in permisos)
        {
            if (rol.Permisos.All(vinculo => vinculo.PermisoId != permiso.Id))
            {
                db.Set<RolPermiso>().Add(new RolPermiso { RolId = rol.Id, PermisoId = permiso.Id });
            }
        }

        await AsegurarRolAsync(db, permisos, PermisosNucleo.RolUsuario, "Usuario", [], ct);
        await AsegurarRolAsync(db, permisos, PermisosNucleo.RolJefeArea, "Jefe de área", [PermisosNucleo.UsuariosConsultar, PermisosNucleo.UsuariosAprobar], ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task AsegurarRolAsync(CentroDbContext db, List<Permiso> permisos, string codigo, string nombre, IReadOnlyList<string> codigosPermiso, CancellationToken ct)
    {
        var rol = await db.Roles.Include(item => item.Permisos).FirstOrDefaultAsync(item => item.Codigo == codigo, ct);
        if (rol is null)
        {
            rol = new Rol { Id = Guid.NewGuid(), Codigo = codigo, Nombre = nombre };
            db.Roles.Add(rol);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            rol.Nombre = nombre;
        }

        foreach (var codigoPermiso in codigosPermiso)
        {
            var permiso = permisos.FirstOrDefault(item => item.Codigo == codigoPermiso);
            if (permiso is null || rol.Permisos.Any(vinculo => vinculo.PermisoId == permiso.Id))
            {
                continue;
            }

            db.Set<RolPermiso>().Add(new RolPermiso { RolId = rol.Id, PermisoId = permiso.Id });
        }
    }
}
