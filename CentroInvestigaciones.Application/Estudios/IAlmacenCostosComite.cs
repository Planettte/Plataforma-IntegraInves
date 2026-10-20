using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Estudios;

public interface IAlmacenCostosComite
{
    CostosComite Leer();

    void Guardar(CostosComite costos);
}
