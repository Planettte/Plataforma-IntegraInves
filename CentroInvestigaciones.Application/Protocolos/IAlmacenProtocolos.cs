using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Protocolos;

public interface IAlmacenProtocolos
{
    IReadOnlyList<Protocolo> Listar();

    Protocolo? Buscar(Guid id);

    Protocolo? BuscarPorToken(string token);

    Protocolo Crear(Protocolo protocolo);

    Protocolo Reenviar(string token, Protocolo protocolo);

    Protocolo Corregir(Guid id, IReadOnlyDictionary<string, string> comentarios);

    Protocolo Aceptar(Guid id);
}
