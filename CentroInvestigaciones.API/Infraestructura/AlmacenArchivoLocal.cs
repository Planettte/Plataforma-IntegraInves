using System.Text.Json;
using System.Text.Json.Serialization;
using CentroInvestigaciones.Application.Nucleo;
using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public sealed class AlmacenArchivoLocal : IAlmacenNucleo
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly string _ruta;
    private readonly Lock _bloqueo = new();
    private readonly List<Area> _areas = [];
    private readonly List<Usuario> _usuarios = [];
    private readonly List<Rol> _roles = [];
    private readonly List<Permiso> _permisos = [];
    private readonly List<RolPermiso> _rolesPermisos = [];
    private readonly List<AsignacionRol> _asignaciones = [];
    private readonly List<AsignacionPermiso> _permisosDirectos = [];
    private readonly List<EventoAuditoria> _eventos = [];
    private readonly List<VinculoArea> _areasUsuario = [];
    private readonly List<Nota> _notas = [];

    public AlmacenArchivoLocal(IWebHostEnvironment entorno)
    {
        var carpeta = Path.Combine(entorno.ContentRootPath, "datos-locales");
        Directory.CreateDirectory(carpeta);
        _ruta = Path.Combine(carpeta, "nucleo.json");
        Cargar();
        Sembrar();
        Enlazar();
        GuardarSinBloqueo();
    }

    public bool Disponible => true;

    public Task<Usuario?> BuscarPorProveedorAsync(string identificador, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.IdentificadorProveedor == identificador));
        }
    }

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_usuarios.FirstOrDefault(usuario => usuario.Id == id));
        }
    }

    public Task<IReadOnlyList<Area>> ListarAreasAsync(CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult<IReadOnlyList<Area>>(_areas.OrderBy(area => area.Nombre).ToList());
        }
    }

    public Task<Area?> BuscarAreaAsync(Guid id, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_areas.FirstOrDefault(area => area.Id == id));
        }
    }

    public Task AgregarUsuarioAsync(Usuario usuario, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            _usuarios.Add(usuario);
            Enlazar();
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExisteAdministradorPlataformaAsync(CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_asignaciones.Any(asignacion =>
                asignacion.AreaId is null &&
                _roles.Any(rol => rol.Id == asignacion.RolId && rol.Codigo == PermisosNucleo.RolAdministrador)));
        }
    }

    public Task<Rol?> BuscarRolPorCodigoAsync(string codigo, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_roles.FirstOrDefault(rol => rol.Codigo == codigo));
        }
    }

    public Task<Rol?> BuscarRolPorIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_roles.FirstOrDefault(rol => rol.Id == id));
        }
    }

    public Task<bool> ExisteAsignacionAsync(Guid usuarioId, Guid rolId, Guid? areaId, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_asignaciones.Any(asignacion =>
                asignacion.UsuarioId == usuarioId && asignacion.RolId == rolId && asignacion.AreaId == areaId));
        }
    }

    public Task AgregarAsignacionAsync(AsignacionRol asignacion, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            _asignaciones.Add(asignacion);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> PermisosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            var roles = _asignaciones
                .Where(asignacion => asignacion.UsuarioId == usuarioId)
                .Select(asignacion => asignacion.RolId)
                .ToHashSet();
            var porRol = _rolesPermisos
                .Where(vinculo => roles.Contains(vinculo.RolId))
                .Select(vinculo => _permisos.First(permiso => permiso.Id == vinculo.PermisoId).Codigo);
            var directos = _permisosDirectos
                .Where(vinculo => vinculo.UsuarioId == usuarioId)
                .Select(vinculo => _permisos.First(permiso => permiso.Id == vinculo.PermisoId).Codigo);
            var codigos = porRol.Concat(directos).Distinct(StringComparer.Ordinal).ToList();
            var usuario = _usuarios.FirstOrDefault(item => item.Id == usuarioId);
            var area = usuario?.AreaInteres?.Codigo ?? _areas.FirstOrDefault(item => item.Id == usuario?.AreaInteresId)?.Codigo;
            var agenda = AccesoAgenda.PermisoPorCuenta(area, usuario?.Cargo);
            if (agenda is not null && !codigos.Contains(agenda, StringComparer.Ordinal))
            {
                codigos.Add(agenda);
            }

            return Task.FromResult<IReadOnlyList<string>>(codigos);
        }
    }

    public Task<IReadOnlyList<string>> PermisosDirectosDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            var codigos = _permisosDirectos
                .Where(vinculo => vinculo.UsuarioId == usuarioId)
                .Select(vinculo => _permisos.First(permiso => permiso.Id == vinculo.PermisoId).Codigo)
                .ToList();
            return Task.FromResult<IReadOnlyList<string>>(codigos);
        }
    }

    public Task ReemplazarPermisosDirectosAsync(Guid usuarioId, IReadOnlyList<string> codigos, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            _permisosDirectos.RemoveAll(vinculo => vinculo.UsuarioId == usuarioId);
            var herramientas = CatalogoHerramientas.Todas.Select(item => item.CodigoPermiso).ToHashSet(StringComparer.Ordinal);
            foreach (var codigo in codigos.Where(item => herramientas.Contains(item)).Distinct(StringComparer.Ordinal))
            {
                var permiso = _permisos.FirstOrDefault(item => item.Codigo == codigo);
                if (permiso is null)
                {
                    continue;
                }

                _permisosDirectos.Add(new AsignacionPermiso { UsuarioId = usuarioId, PermisoId = permiso.Id });
            }

            Enlazar();
        }

        return Task.CompletedTask;
    }

    public Task ReemplazarRolPlataformaAsync(Guid usuarioId, string codigoRol, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            var rol = _roles.FirstOrDefault(item => item.Codigo == codigoRol);
            if (rol is null)
            {
                return Task.CompletedTask;
            }

            var rolesPlataforma = _roles
                .Where(item => PermisosNucleo.RolesDePlataforma.Contains(item.Codigo, StringComparer.Ordinal))
                .Select(item => item.Id)
                .ToHashSet();
            _asignaciones.RemoveAll(asignacion => asignacion.UsuarioId == usuarioId && rolesPlataforma.Contains(asignacion.RolId));
            _asignaciones.Add(new AsignacionRol
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                RolId = rol.Id
            });
            Enlazar();
        }

        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<Usuario> Elementos, int Total)> ListarUsuariosAsync(int pagina, int tamano, string? busqueda, string? estado = null, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            IEnumerable<Usuario> consulta = _usuarios;
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var texto = busqueda.Trim();
                consulta = consulta.Where(usuario =>
                    usuario.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    usuario.CorreoElectronico.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoCuenta>(estado, out var estadoCuenta))
            {
                consulta = consulta.Where(usuario => usuario.Estado == estadoCuenta);
            }

            var lista = consulta.OrderBy(usuario => usuario.Nombre).ToList();
            return Task.FromResult<(IReadOnlyList<Usuario>, int)>((lista.Skip(pagina * tamano).Take(tamano).ToList(), lista.Count));
        }
    }

    public Task<IReadOnlyList<Rol>> ListarRolesAsync(CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult<IReadOnlyList<Rol>>(_roles.OrderBy(rol => rol.Nombre).ToList());
        }
    }

    public Task<IReadOnlyList<Permiso>> ListarPermisosAsync(CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult<IReadOnlyList<Permiso>>(_permisos.OrderBy(permiso => permiso.Codigo).ToList());
        }
    }

    public Task AgregarEventoAsync(EventoAuditoria evento, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            _eventos.Add(evento);
        }

        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<EventoAuditoria> Elementos, int Total)> ListarEventosAsync(int pagina, int tamano, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            var ordenados = _eventos.OrderByDescending(evento => evento.FechaUtc).ToList();
            return Task.FromResult<(IReadOnlyList<EventoAuditoria>, int)>((ordenados.Skip(pagina * tamano).Take(tamano).ToList(), ordenados.Count));
        }
    }

    public Task<IReadOnlyDictionary<Guid, string>> NombresUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            var conjunto = ids.ToHashSet();
            var nombres = _usuarios
                .Where(usuario => conjunto.Contains(usuario.Id))
                .ToDictionary(usuario => usuario.Id, usuario => usuario.Nombre);
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(nombres);
        }
    }

    public Task<IReadOnlyList<Nota>> ListarNotasAsync(Guid usuarioId, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            var lista = _notas
                .Where(nota => nota.UsuarioId == usuarioId)
                .OrderByDescending(nota => nota.FechaActualizacionUtc ?? nota.FechaCreacionUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<Nota>>(lista);
        }
    }

    public Task<Nota?> BuscarNotaAsync(Guid id, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_notas.FirstOrDefault(nota => nota.Id == id));
        }
    }

    public Task AgregarNotaAsync(Nota nota, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            _notas.Add(nota);
        }

        return Task.CompletedTask;
    }

    public Task EliminarNotaAsync(Nota nota, CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            _notas.RemoveAll(item => item.Id == nota.Id);
        }

        return Task.CompletedTask;
    }

    public Task GuardarAsync(CancellationToken ct = default)
    {
        lock (_bloqueo)
        {
            GuardarSinBloqueo();
        }

        return Task.CompletedTask;
    }

    private void Sembrar()
    {
        var areas = new (string Codigo, string Nombre)[]
        {
            ("estudios", "Estudios clínicos"),
            ("udd", "Diseño y desarrollo"),
            ("comite", "Investigaciones")
        };

        foreach (var (codigo, nombre) in areas)
        {
            if (_areas.All(area => area.Codigo != codigo))
            {
                _areas.Add(new Area { Id = Guid.NewGuid(), Codigo = codigo, Nombre = nombre });
            }
        }

        foreach (var (codigo, nombre) in PermisosNucleo.Catalogo)
        {
            if (_permisos.All(permiso => permiso.Codigo != codigo))
            {
                _permisos.Add(new Permiso { Id = Guid.NewGuid(), Codigo = codigo, Nombre = nombre });
            }
        }

        var codigosValidos = PermisosNucleo.Catalogo.Select(item => item.Codigo).ToHashSet(StringComparer.Ordinal);
        var permisosObsoletos = _permisos.Where(permiso => !codigosValidos.Contains(permiso.Codigo)).ToList();
        foreach (var permiso in permisosObsoletos)
        {
            _rolesPermisos.RemoveAll(vinculo => vinculo.PermisoId == permiso.Id);
            _permisosDirectos.RemoveAll(vinculo => vinculo.PermisoId == permiso.Id);
            _permisos.Remove(permiso);
        }

        var rol = AsegurarRol(PermisosNucleo.RolAdministrador, "Administrador");
        foreach (var permiso in _permisos)
        {
            if (_rolesPermisos.All(vinculo => vinculo.RolId != rol.Id || vinculo.PermisoId != permiso.Id))
            {
                _rolesPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = permiso.Id });
            }
        }

        AsegurarRol(PermisosNucleo.RolUsuario, "Usuario");
        AsegurarRol(PermisosNucleo.RolJefeArea, "Jefe de área", PermisosNucleo.UsuariosConsultar, PermisosNucleo.UsuariosAprobar);
    }

    private Rol AsegurarRol(string codigo, string nombre, params string[] permisos)
    {
        var rol = _roles.FirstOrDefault(item => item.Codigo == codigo);
        if (rol is null)
        {
            rol = new Rol { Id = Guid.NewGuid(), Codigo = codigo, Nombre = nombre };
            _roles.Add(rol);
        }
        else
        {
            rol.Nombre = nombre;
        }

        foreach (var codigoPermiso in permisos)
        {
            var permiso = _permisos.FirstOrDefault(item => item.Codigo == codigoPermiso);
            if (permiso is null || _rolesPermisos.Any(vinculo => vinculo.RolId == rol.Id && vinculo.PermisoId == permiso.Id))
            {
                continue;
            }

            _rolesPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = permiso.Id });
        }

        return rol;
    }

    private void CapturarAreas()
    {
        foreach (var usuario in _usuarios)
        {
            foreach (var vinculo in usuario.Areas)
            {
                if (_areasUsuario.All(item => item.UsuarioId != usuario.Id || item.AreaId != vinculo.AreaId))
                {
                    _areasUsuario.Add(new VinculoArea { UsuarioId = usuario.Id, AreaId = vinculo.AreaId });
                }
            }
        }
    }

    private void Enlazar()
    {
        CapturarAreas();

        foreach (var usuario in _usuarios)
        {
            usuario.AreaInteres = _areas.FirstOrDefault(area => area.Id == usuario.AreaInteresId);
            usuario.Areas = _areasUsuario
                .Where(vinculo => vinculo.UsuarioId == usuario.Id)
                .Select(vinculo => new UsuarioArea
                {
                    UsuarioId = usuario.Id,
                    Usuario = usuario,
                    AreaId = vinculo.AreaId,
                    Area = _areas.First(area => area.Id == vinculo.AreaId)
                })
                .ToList();
            usuario.Asignaciones = _asignaciones.Where(asignacion => asignacion.UsuarioId == usuario.Id).ToList();
            foreach (var asignacion in usuario.Asignaciones)
            {
                asignacion.Usuario = usuario;
                asignacion.Rol = _roles.First(rol => rol.Id == asignacion.RolId);
            }
            usuario.PermisosDirectos = _permisosDirectos
                .Where(vinculo => vinculo.UsuarioId == usuario.Id)
                .Select(vinculo => new AsignacionPermiso
                {
                    UsuarioId = usuario.Id,
                    Usuario = usuario,
                    PermisoId = vinculo.PermisoId,
                    Permiso = _permisos.First(permiso => permiso.Id == vinculo.PermisoId)
                })
                .ToList();
        }

        foreach (var rol in _roles)
        {
            rol.Permisos = _rolesPermisos.Where(vinculo => vinculo.RolId == rol.Id).ToList();
            foreach (var vinculo in rol.Permisos)
            {
                vinculo.Rol = rol;
                vinculo.Permiso = _permisos.First(permiso => permiso.Id == vinculo.PermisoId);
            }
        }
    }

    private void SoltarNavegacion()
    {
        foreach (var usuario in _usuarios)
        {
            usuario.AreaInteres = null;
            usuario.Areas = [];
            usuario.Asignaciones = [];
            usuario.PermisosDirectos = [];
        }

        foreach (var rol in _roles)
        {
            rol.Permisos = [];
        }

        foreach (var permiso in _permisos)
        {
            permiso.Roles = [];
        }

        foreach (var vinculo in _rolesPermisos)
        {
            vinculo.Rol = null!;
            vinculo.Permiso = null!;
        }

        foreach (var asignacion in _asignaciones)
        {
            asignacion.Usuario = null!;
            asignacion.Rol = null!;
            asignacion.Area = null;
        }

        foreach (var vinculo in _permisosDirectos)
        {
            vinculo.Usuario = null!;
            vinculo.Permiso = null!;
        }
    }

    private void Cargar()
    {
        if (!File.Exists(_ruta))
        {
            return;
        }

        var foto = JsonSerializer.Deserialize<FotoLocal>(File.ReadAllText(_ruta), Json);
        if (foto is null)
        {
            return;
        }

        _areas.AddRange(foto.Areas);
        _usuarios.AddRange(foto.Usuarios);
        _roles.AddRange(foto.Roles);
        _permisos.AddRange(foto.Permisos);
        _rolesPermisos.AddRange(foto.RolesPermisos);
        _asignaciones.AddRange(foto.Asignaciones);
        _permisosDirectos.AddRange(foto.PermisosDirectos);
        _eventos.AddRange(foto.Eventos);
        _areasUsuario.AddRange(foto.AreasUsuario);
        _notas.AddRange(foto.Notas);
    }

    private void GuardarSinBloqueo()
    {
        CapturarAreas();
        SoltarNavegacion();

        var foto = new FotoLocal
        {
            Areas = _areas,
            Usuarios = _usuarios,
            Roles = _roles,
            Permisos = _permisos,
            RolesPermisos = _rolesPermisos,
            Asignaciones = _asignaciones,
            PermisosDirectos = _permisosDirectos.Select(vinculo => new AsignacionPermiso
            {
                UsuarioId = vinculo.UsuarioId,
                PermisoId = vinculo.PermisoId
            }).ToList(),
            Eventos = _eventos,
            AreasUsuario = _areasUsuario,
            Notas = _notas.Select(nota => new Nota
            {
                Id = nota.Id,
                UsuarioId = nota.UsuarioId,
                Titulo = nota.Titulo,
                Contenido = nota.Contenido,
                FechaCreacionUtc = nota.FechaCreacionUtc,
                FechaActualizacionUtc = nota.FechaActualizacionUtc
            }).ToList()
        };

        EscrituraJson.Guardar(_ruta, JsonSerializer.Serialize(foto, Json));
        Enlazar();
    }

    private sealed class VinculoArea
    {
        public Guid UsuarioId { get; set; }

        public Guid AreaId { get; set; }
    }

    private sealed class FotoLocal
    {
        public List<Area> Areas { get; set; } = [];

        public List<Usuario> Usuarios { get; set; } = [];

        public List<Rol> Roles { get; set; } = [];

        public List<Permiso> Permisos { get; set; } = [];

        public List<RolPermiso> RolesPermisos { get; set; } = [];

        public List<AsignacionRol> Asignaciones { get; set; } = [];

        public List<AsignacionPermiso> PermisosDirectos { get; set; } = [];

        public List<EventoAuditoria> Eventos { get; set; } = [];

        public List<VinculoArea> AreasUsuario { get; set; } = [];

        public List<Nota> Notas { get; set; } = [];
    }
}
