namespace CentroInvestigaciones.API.Infraestructura;

internal static class EscrituraJson
{
    public static void Guardar(string ruta, string contenido)
    {
        var temporal = ruta + ".tmp";
        File.WriteAllText(temporal, contenido);
        File.Move(temporal, ruta, overwrite: true);
    }
}
