using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Farmacia;

public interface IAlmacenFarmacia
{
    IReadOnlyList<FormatoFarmacia> ListarFormatos();

    FormatoFarmacia? BuscarFormato(Guid id);

    FormatoFarmacia GuardarFormato(FormatoFarmacia formato);

    bool DesactivarFormato(Guid id, Guid? usuarioId, string nombre);

    IReadOnlyList<FilaFarmacia> ListarInventario();

    FilaFarmacia GuardarInventario(FilaFarmacia fila);

    bool EliminarInventario(Guid id);
}
