using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.HojasVida;

public interface IAlmacenHojasVida
{
    IReadOnlyList<HojaVida> Listar();

    HojaVida? Buscar(Guid id);

    HojaVida Crear(string nombreCompleto, string tituloAcademico, string usuario);

    (HojaVida Hoja, bool VersionNueva) Actualizar(Guid id, HojaVida datos, string usuario);

    bool Eliminar(Guid id);

    SoporteHoja AgregarSoporte(Guid hojaId, string nombre, string tipo, Stream contenido, string usuario);

    void ReordenarSoportes(Guid hojaId, IReadOnlyList<Guid> orden, string usuario);

    bool EliminarSoporte(Guid hojaId, Guid soporteId, string usuario);

    void GuardarFirma(Guid hojaId, string nombre, string tipo, string fecha, Stream contenido, string usuario);

    bool QuitarFirma(Guid hojaId, string usuario);

    Stream AbrirSoporte(Guid hojaId, Guid soporteId);

    Stream? AbrirFirma(Guid hojaId);
}
