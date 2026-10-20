namespace CentroInvestigaciones.Client.Navigation;

public sealed record ElementoNavegacion(
    string Titulo,
    string Ruta,
    string? Descripcion = null,
    IReadOnlyList<ElementoNavegacion>? Hijos = null,
    string? Subtitulo = null,
    string? Etiqueta = null,
    string? Icono = null,
    string? CodigoPermiso = null);

public static class CatalogoNavegacion
{
    public const string MensajeHerramienta = "Este módulo está pendiente de análisis funcional.";

    public static IReadOnlyList<ElementoNavegacion> Herramientas { get; } =
    [
        new("Hojas de Vida", "/estudios-clinicos/hojas-de-vida", Icono: "documento", CodigoPermiso: "herramientas.estudios.hojas-de-vida", Etiqueta: "Estudios Clínicos"),
        new("Plantillas de estudios", "/estudios-clinicos/plantillas", Icono: "formulario", CodigoPermiso: "herramientas.estudios.plantillas", Etiqueta: "Estudios Clínicos"),
        new("Inventario Farmacia", "/estudios-clinicos/inventario-farmacia", Icono: "frasco", CodigoPermiso: "herramientas.estudios.inventario-farmacia", Etiqueta: "Estudios Clínicos"),
        new("Agenda Investigaciones", "/estudios-clinicos/agenda", Subtitulo: "Coordinación y Médicos", Icono: "agenda", CodigoPermiso: "herramientas.estudios.agenda", Etiqueta: "Estudios Clínicos"),
        new("Retención Documental", "/estudios-clinicos/retencion-documental", Icono: "archivo", CodigoPermiso: "herramientas.estudios.retencion-documental", Etiqueta: "Estudios Clínicos"),
        new("Dispositivos Médicos", "/estudios-clinicos/inventario-dispositivos", Icono: "dispositivo", CodigoPermiso: "herramientas.estudios.dispositivos", Etiqueta: "Estudios Clínicos"),
        new("Agenda UDD", "/udd/agenda", Icono: "agenda", CodigoPermiso: "herramientas.udd.agenda", Etiqueta: "Diseño y desarrollo"),
        new("Formulario Protocolos", "/udd/protocolos", Icono: "formulario", CodigoPermiso: "herramientas.udd.protocolos", Etiqueta: "Diseño y desarrollo"),
        new("Seguimiento de Proyectos", "/udd/proyectos", Icono: "proyecto", CodigoPermiso: "herramientas.udd.proyectos", Etiqueta: "Diseño y desarrollo"),
        new("Estudios Académicos/Clínicos", "/comite/estudios", Icono: "estudio", CodigoPermiso: "herramientas.comite.estudios", Etiqueta: "Comité de investigaciones")
    ];

    public static IReadOnlyList<ElementoNavegacion> Principal { get; } =
    [
        new("Inicio", "/"),
        new(
            "Estudios Clínicos",
            "/estudios-clinicos",
            "Herramientas del área clínica.",
            Herramientas.Where(item => item.Etiqueta == "Estudios Clínicos").ToList(),
            Etiqueta: "Área"),
        new(
            "Diseño y desarrollo",
            "/udd",
            "Unidad de Diseño y Desarrollo.",
            Herramientas.Where(item => item.Etiqueta == "Diseño y desarrollo").ToList(),
            Etiqueta: "Unidad"),
        new(
            "Comité de investigaciones",
            "/comite",
            "Comité de investigaciones.",
            Herramientas.Where(item => item.Etiqueta == "Comité de investigaciones").ToList(),
            Etiqueta: "Comité")
    ];

    public static IReadOnlyList<ElementoNavegacion> Administracion { get; } =
    [
        new("Usuarios", "/administracion/usuarios", "Cuentas, aprobación y acceso."),
    ];

    public static IReadOnlyList<ElementoNavegacion> Cuenta { get; } =
    [
        new("Perfil", "/perfil", "Identidad e información de la cuenta."),
        new("Estado de cuenta", "/estado-cuenta", "Situación de acceso a la plataforma.")
    ];

    public static IReadOnlyList<ElementoNavegacion> HerramientasVisibles(IEnumerable<string> permisos, string? cargo = null)
    {
        var conjunto = permisos.ToHashSet(StringComparer.Ordinal);
        var consulta = cargo is "apoyo" or "farmacia" && !conjunto.Contains("nucleo.usuarios.modificar");
        return Herramientas
            .Where(item => item.CodigoPermiso is not null && conjunto.Contains(item.CodigoPermiso))
            .Select(item => consulta && item.CodigoPermiso == "herramientas.estudios.agenda"
                ? item with { Ruta = "/agenda/general", Subtitulo = "Consulta" }
                : item)
            .ToList();
    }

    public static IReadOnlyList<ElementoNavegacion> AdministracionVisible(IEnumerable<string> permisos)
    {
        var conjunto = permisos.ToHashSet(StringComparer.Ordinal);
        return Administracion.Where(item => item.Ruta switch
        {
            "/administracion/usuarios" => conjunto.Contains("nucleo.usuarios.consultar") || conjunto.Contains("nucleo.usuarios.aprobar"),
            _ => conjunto.Contains("nucleo.usuarios.modificar")
        }).ToList();
    }

    public static (ElementoNavegacion Elemento, ElementoNavegacion? Area)? Buscar(string ruta)
    {
        foreach (var seccion in new[] { Principal, Administracion, Cuenta })
        {
            foreach (var item in seccion)
            {
                if (RutasIguales(item.Ruta, ruta))
                {
                    return (item, null);
                }

                if (item.Hijos is null)
                {
                    continue;
                }

                foreach (var hijo in item.Hijos)
                {
                    if (RutasIguales(hijo.Ruta, ruta))
                    {
                        return (hijo, item);
                    }
                }
            }
        }

        return null;
    }

    private static bool RutasIguales(string izquierda, string derecha)
    {
        return string.Equals(izquierda.TrimEnd('/'), derecha.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}
