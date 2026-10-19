using CentroInvestigaciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentroInvestigaciones.Data;

public class CentroDbContext : DbContext
{
    public CentroDbContext(DbContextOptions<CentroDbContext> options)
        : base(options)
    {
    }

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Rol> Roles => Set<Rol>();

    public DbSet<Permiso> Permisos => Set<Permiso>();

    public DbSet<AsignacionRol> AsignacionesRol => Set<AsignacionRol>();

    public DbSet<EventoAuditoria> EventosAuditoria => Set<EventoAuditoria>();

    public DbSet<Nota> Notas => Set<Nota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquemas.Nucleo);

        modelBuilder.Entity<Area>(entidad =>
        {
            entidad.ToTable("Areas");
            entidad.HasIndex(area => area.Codigo).IsUnique();
            entidad.Property(area => area.Codigo).HasMaxLength(40);
            entidad.Property(area => area.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<Usuario>(entidad =>
        {
            entidad.ToTable("Usuarios");
            entidad.HasIndex(usuario => usuario.IdentificadorProveedor).IsUnique();
            entidad.Property(usuario => usuario.IdentificadorProveedor).HasMaxLength(128);
            entidad.Property(usuario => usuario.CorreoElectronico).HasMaxLength(256);
            entidad.Property(usuario => usuario.Nombre).HasMaxLength(200);
            entidad.Property(usuario => usuario.Nombres).HasMaxLength(120);
            entidad.Property(usuario => usuario.Apellidos).HasMaxLength(120);
            entidad.Property(usuario => usuario.NumeroContacto).HasMaxLength(40);
            entidad.Property(usuario => usuario.Extension).HasMaxLength(20);
            entidad.Property(usuario => usuario.Cargo).HasMaxLength(120);
            entidad.Property(usuario => usuario.TemaPreferido).HasMaxLength(40);
            entidad.Property(usuario => usuario.Estado).HasConversion<string>().HasMaxLength(40);
            entidad.HasOne(usuario => usuario.AreaInteres)
                .WithMany()
                .HasForeignKey(usuario => usuario.AreaInteresId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UsuarioArea>(entidad =>
        {
            entidad.ToTable("UsuariosAreas");
            entidad.HasKey(vinculo => new { vinculo.UsuarioId, vinculo.AreaId });
            entidad.HasOne(vinculo => vinculo.Usuario)
                .WithMany(usuario => usuario.Areas)
                .HasForeignKey(vinculo => vinculo.UsuarioId);
            entidad.HasOne(vinculo => vinculo.Area)
                .WithMany()
                .HasForeignKey(vinculo => vinculo.AreaId);
        });

        modelBuilder.Entity<Rol>(entidad =>
        {
            entidad.ToTable("Roles");
            entidad.HasIndex(rol => rol.Codigo).IsUnique();
            entidad.Property(rol => rol.Codigo).HasMaxLength(80);
            entidad.Property(rol => rol.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<Permiso>(entidad =>
        {
            entidad.ToTable("Permisos");
            entidad.HasIndex(permiso => permiso.Codigo).IsUnique();
            entidad.Property(permiso => permiso.Codigo).HasMaxLength(80);
            entidad.Property(permiso => permiso.Nombre).HasMaxLength(160);
        });

        modelBuilder.Entity<RolPermiso>(entidad =>
        {
            entidad.ToTable("RolesPermisos");
            entidad.HasKey(vinculo => new { vinculo.RolId, vinculo.PermisoId });
            entidad.HasOne(vinculo => vinculo.Rol)
                .WithMany(rol => rol.Permisos)
                .HasForeignKey(vinculo => vinculo.RolId);
            entidad.HasOne(vinculo => vinculo.Permiso)
                .WithMany(permiso => permiso.Roles)
                .HasForeignKey(vinculo => vinculo.PermisoId);
        });

        modelBuilder.Entity<AsignacionRol>(entidad =>
        {
            entidad.ToTable("AsignacionesRol");
            entidad.HasOne(asignacion => asignacion.Usuario)
                .WithMany(usuario => usuario.Asignaciones)
                .HasForeignKey(asignacion => asignacion.UsuarioId);
            entidad.HasOne(asignacion => asignacion.Rol)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.RolId);
            entidad.HasOne(asignacion => asignacion.Area)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.AreaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AsignacionPermiso>(entidad =>
        {
            entidad.ToTable("AsignacionesPermiso");
            entidad.HasKey(vinculo => new { vinculo.UsuarioId, vinculo.PermisoId });
            entidad.HasOne(vinculo => vinculo.Usuario)
                .WithMany(usuario => usuario.PermisosDirectos)
                .HasForeignKey(vinculo => vinculo.UsuarioId);
            entidad.HasOne(vinculo => vinculo.Permiso)
                .WithMany()
                .HasForeignKey(vinculo => vinculo.PermisoId);
        });

        modelBuilder.Entity<EventoAuditoria>(entidad =>
        {
            entidad.ToTable("EventosAuditoria");
            entidad.Property(evento => evento.Accion).HasMaxLength(80);
            entidad.Property(evento => evento.Entidad).HasMaxLength(80);
            entidad.Property(evento => evento.EntidadId).HasMaxLength(64);
            entidad.Property(evento => evento.Resultado).HasMaxLength(40);
            entidad.Property(evento => evento.DireccionRed).HasMaxLength(64);
            entidad.Property(evento => evento.AgenteUsuario).HasMaxLength(256);
            entidad.HasIndex(evento => evento.FechaUtc);
        });

        modelBuilder.Entity<Nota>(entidad =>
        {
            entidad.ToTable("Notas");
            entidad.Property(nota => nota.Titulo).HasMaxLength(160);
            entidad.Property(nota => nota.Contenido).HasMaxLength(4000);
            entidad.HasOne(nota => nota.Usuario)
                .WithMany()
                .HasForeignKey(nota => nota.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entidad.HasIndex(nota => nota.UsuarioId);
        });
    }
}
