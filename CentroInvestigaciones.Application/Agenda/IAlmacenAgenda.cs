using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Agenda;

public interface IAlmacenAgenda
{
    IReadOnlyList<CitaAgenda> Citas();

    IReadOnlyList<MonitoriaAgenda> Monitorias();

    IReadOnlyList<DateOnly> Festivos();

    void GuardarCita(CitaAgenda cita);

    bool EliminarCita(Guid id);

    void GuardarMonitoria(MonitoriaAgenda monitoria);

    bool EliminarMonitoria(Guid id);
}
