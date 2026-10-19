namespace CentroInvestigaciones.Application.Plantillas;

public interface IFirmasInvestigador
{
    byte[]? Buscar(string nombre, string titulo);

    void Asegurar(string nombre, string titulo);

    void Renombrar(string nombreAnterior, string tituloAnterior, string nombre, string titulo);

    void Eliminar(string nombre, string titulo);
}
