namespace CentroInvestigaciones.Application.HojasVida;

public interface IDepositoArchivos
{
    string Guardar(Guid hojaId, string carpeta, string nombreArchivo, Stream contenido);

    Stream Abrir(string ruta);

    void Eliminar(string ruta);

    void EliminarHoja(Guid hojaId);
}
