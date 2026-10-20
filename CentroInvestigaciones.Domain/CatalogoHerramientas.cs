namespace CentroInvestigaciones.Domain;

public sealed record DefinicionHerramienta(
    string CodigoPermiso,
    string AreaCodigo,
    string AreaNombre,
    string Titulo,
    string Ruta,
    string? Icono = null);

public static class CatalogoHerramientas
{
    public static IReadOnlyList<DefinicionHerramienta> Todas { get; } =
    [
        new("herramientas.estudios.hojas-de-vida", "estudios", "Estudios Clínicos", "Hojas de Vida", "/estudios-clinicos/hojas-de-vida", "documento"),
        new("herramientas.estudios.plantillas", "estudios", "Estudios Clínicos", "Plantillas de estudios", "/estudios-clinicos/plantillas", "formulario"),
        new("herramientas.estudios.inventario-farmacia", "estudios", "Estudios Clínicos", "Inventario Farmacia", "/estudios-clinicos/inventario-farmacia", "frasco"),
        new("herramientas.estudios.agenda", "estudios", "Estudios Clínicos", "Agenda Investigaciones", "/estudios-clinicos/agenda", "agenda"),
        new("herramientas.estudios.retencion-documental", "estudios", "Estudios Clínicos", "Retención Documental", "/estudios-clinicos/retencion-documental", "archivo"),
        new("herramientas.estudios.dispositivos", "estudios", "Estudios Clínicos", "Dispositivos Médicos", "/estudios-clinicos/inventario-dispositivos", "dispositivo"),
        new("herramientas.udd.agenda", "udd", "Diseño y desarrollo", "Agenda UDD", "/udd/agenda", "agenda"),
        new("herramientas.udd.protocolos", "udd", "Diseño y desarrollo", "Formulario Protocolos", "/udd/protocolos", "formulario"),
        new("herramientas.udd.proyectos", "udd", "Diseño y desarrollo", "Seguimiento de Proyectos", "/udd/proyectos", "proyecto"),
        new("herramientas.comite.estudios", "comite", "Comité de investigaciones", "Estudios Académicos/Clínicos", "/comite/estudios", "estudio")
    ];

    public static IEnumerable<IGrouping<string, DefinicionHerramienta>> PorArea() =>
        Todas.GroupBy(item => item.AreaCodigo);
}
