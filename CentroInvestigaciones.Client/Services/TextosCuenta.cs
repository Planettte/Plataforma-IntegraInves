namespace CentroInvestigaciones.Client.Services;

public static class TextosCuenta
{
    public static string Estado(string? codigo) => codigo switch
    {
        "PendienteAprobacion" => "Pendiente de aprobación",
        "Activo" => "Activo",
        "Rechazado" => "Rechazado",
        "Inactivo" => "Inactivo",
        _ => "Sin registro"
    };

    public static string Tono(string? codigo) => codigo switch
    {
        "Activo" => "exito",
        "PendienteAprobacion" => "aviso",
        "Rechazado" => "peligro",
        _ => "neutro"
    };
}
