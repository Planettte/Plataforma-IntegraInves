using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Estudios;

public interface IAlmacenEstudios
{
    IReadOnlyList<Estudio> Listar();

    Estudio? Buscar(Guid id);

    Estudio? BuscarPorToken(string token);

    Estudio Crear(Estudio datos);

    Estudio? ActualizarFase1(string token, Estudio datos);

    ArchivoEstudio? AgregarArchivo(Guid estudioId, string clave, string nombre, string tipoContenido, Stream contenido);

    void QuitarPorClave(Guid estudioId, string clave);

    Estudio? SolicitarCorreccion(Guid id, string mensaje);

    Estudio? EmitirAval(Guid id, string mensaje);

    Estudio? GuardarFase2(string token, string numeroAprobacionCei);

    Estudio? Finalizar(Guid id);

    void MarcarAvisoRecibido(Guid id, bool enviado);

    Stream? Abrir(Guid estudioId, Guid archivoId);

    Stream? AbrirPorToken(string token, Guid archivoId);
}
