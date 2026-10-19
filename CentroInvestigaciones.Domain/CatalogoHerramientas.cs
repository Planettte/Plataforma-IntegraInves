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
        new("herramientas.estudios.hojas-de-vida", "estudios", "Estudios clínicos", "Hojas de Vida", "/estudios-clinicos/hojas-de-vida", "documento"),
        new("herramientas.estudios.plantillas", "estudios", "Estudios clínicos", "Plantillas de estudios", "/estudios-clinicos/plantillas", "formulario"),
        new("herramientas.estudios.inventario-farmacia", "estudios", "Estudios clínicos", "Inventario Farmacia", "/estudios-clinicos/inventario-farmacia", "frasco"),
        new("herramientas.estudios.agenda", "estudios", "Estudios clínicos", "Agenda Investigaciones", "/estudios-clinicos/agenda", "agenda"),
        new("herramientas.estudios.retencion-documental", "estudios", "Estudios clínicos", "Retención Documental", "/estudios-clinicos/retencion-documental", "archivo"),
        new("herramientas.estudios.dispositivos", "estudios", "Estudios clínicos", "Dispositivos Médicos", "/estudios-clinicos/inventario-dispositivos", "dispositivo"),
        new("herramientas.udd.agenda", "udd", "Diseño y desarrollo", "Agenda UDD", "/udd/agenda", "agenda"),
        new("herramientas.udd.protocolos", "udd", "Diseño y desarrollo", "Formulario Protocolos", "/udd/protocolos", "formulario"),
        new("herramientas.udd.proyectos", "udd", "Diseño y desarrollo", "Seguimiento de Proyectos", "/udd/proyectos", "proyecto"),
        new("herramientas.comite.estudios", "comite", "Investigaciones", "Estudios Académicos/Clínicos", "/comite/estudios", "estudio")
    ];

    public static IEnumerable<IGrouping<string, DefinicionHerramienta>> PorArea() =>
        Todas.GroupBy(item => item.AreaCodigo);
}
