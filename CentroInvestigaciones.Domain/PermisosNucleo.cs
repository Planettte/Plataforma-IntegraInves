namespace CentroInvestigaciones.Domain;

public static class PermisosNucleo
{
    public const string UsuariosConsultar = "nucleo.usuarios.consultar";
    public const string UsuariosAprobar = "nucleo.usuarios.aprobar";
    public const string UsuariosModificar = "nucleo.usuarios.modificar";
    public const string RolesConsultar = "nucleo.roles.consultar";
    public const string PermisosConsultar = "nucleo.permisos.consultar";
    public const string AuditoriaConsultar = "nucleo.auditoria.consultar";

    public const string RolUsuario = "usuario";
    public const string RolJefeArea = "jefe-area";
    public const string RolAdministrador = "administrador-plataforma";

    public static IReadOnlyList<string> RolesDePlataforma { get; } = [RolUsuario, RolJefeArea, RolAdministrador];

    public static IReadOnlyList<(string Codigo, string Nombre)> Catalogo { get; } =
    [
        (UsuariosConsultar, "Consultar usuarios"),
        (UsuariosAprobar, "Aprobar o rechazar usuarios"),
        (UsuariosModificar, "Modificar áreas y roles"),
        (RolesConsultar, "Consultar roles"),
        (PermisosConsultar, "Consultar permisos"),
        (AuditoriaConsultar, "Consultar auditoría"),
        ..CatalogoHerramientas.Todas.Select(item => (item.CodigoPermiso, $"Acceso: {item.Titulo}"))
    ];
}
