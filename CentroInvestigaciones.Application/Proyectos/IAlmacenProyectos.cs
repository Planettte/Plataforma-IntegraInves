using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Proyectos;

public interface IAlmacenProyectos
{
    IReadOnlyList<ProyectoSeguimiento> Listar(Guid responsableId);

    ProyectoSeguimiento Crear(ProyectoSeguimiento proyecto, IReadOnlyList<FaseProyecto> fases);

    ProyectoSeguimiento Actualizar(Guid id, string nombre, string semillero);

    void ReemplazarFases(Guid proyectoId, IReadOnlyList<FaseProyecto> fases);

    void GuardarActividades(Guid proyectoId, IReadOnlyList<ActividadProyecto> actividades);

    void CambiarEstado(Guid actividadId, string estado);

    InvestigadorProyecto CrearInvestigador(string nombre);

    void Asignar(Guid actividadId, Guid investigadorId);

    void QuitarAsignacion(Guid actividadId, Guid investigadorId);

    void Prorrogar(Guid actividadId, string nuevaFecha, IReadOnlyList<Guid> investigadores);

    TableroProyecto? Tablero(Guid proyectoId);
}
