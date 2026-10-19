using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.API.Infraestructura;

public static class ConsultaAgenda
{
    public static bool EsDe(CitaAgenda cita, string agenda, Guid? usuarioId, string nombre) =>
        cita.Agenda == agenda
        && (usuarioId is Guid id && cita.UsuarioId == id
            || string.Equals(cita.Persona, nombre, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<CitaAgenda> Medicos(IEnumerable<CitaAgenda> citas) =>
        citas.Where(cita => cita.Agenda == ReglasAgenda.Medicos);

    public static IEnumerable<CitaAgenda> Buscar(IEnumerable<CitaAgenda> citas, string? texto, DateOnly? desde, DateOnly? hasta)
    {
        var clave = texto?.Trim();
        return citas.Where(cita =>
            (desde is null || cita.Fecha >= desde)
            && (hasta is null || cita.Fecha <= hasta)
            && (string.IsNullOrWhiteSpace(clave) || Coincide(cita, clave)))
            .OrderByDescending(cita => cita.Fecha)
            .ThenBy(cita => cita.HoraInicio);
    }

    private static bool Coincide(CitaAgenda cita, string texto)
    {
        if (cita.Persona.Contains(texto, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var campo in new[] { "estudio", "paciente", "nombrePaciente", "codigoPaciente", "visita", "actividad", "participante" })
        {
            if (cita.Campos.TryGetValue(campo, out var valor) && valor.Contains(texto, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
