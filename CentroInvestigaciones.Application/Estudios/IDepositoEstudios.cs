namespace CentroInvestigaciones.Application.Estudios;

public interface IDepositoEstudios
{
    string Guardar(Guid estudioId, string nombreArchivo, Stream contenido);

    Stream Abrir(string ruta);

    void Eliminar(string ruta);
}
