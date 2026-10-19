using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Plantillas;

public interface IAlmacenPlantillas
{
    IReadOnlyList<InvestigadorPlantilla> Investigadores();

    InvestigadorPlantilla AgregarInvestigador(string nombre, string titulo);

    InvestigadorPlantilla? ActualizarInvestigador(Guid id, string nombre, string titulo);

    bool EliminarInvestigador(Guid id);

    IReadOnlyList<CoordinadorPlantilla> Coordinadores();

    CoordinadorPlantilla AgregarCoordinador(string nombre, string correo);

    CoordinadorPlantilla? ActualizarCoordinador(Guid id, string nombre, string correo);

    bool EliminarCoordinador(Guid id);

    IReadOnlyList<EstudioDePlantillas> Estudios();

    EstudioDePlantillas? BuscarEstudio(Guid id);

    EstudioDePlantillas CrearEstudio(EstudioDePlantillas datos);

    EstudioDePlantillas? ActualizarEstudio(Guid id, EstudioDePlantillas datos);

    bool EliminarEstudio(Guid id);

    EstudioDePlantillas? GuardarRevisadas(Guid id, IReadOnlyList<string> codigos);
}
