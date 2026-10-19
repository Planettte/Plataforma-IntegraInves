using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Dispositivos;

public interface IAlmacenDispositivos
{
    IReadOnlyList<DispositivoMedico> Listar();

    DispositivoMedico? Buscar(Guid id);

    DispositivoMedico Crear(DispositivoMedico datos);

    DispositivoMedico? Actualizar(Guid id, DispositivoMedico datos);

    IReadOnlyList<string> Avisos();

    void ReemplazarAvisos(IReadOnlyList<string> claves);

    string CorreoEncargado();

    void GuardarCorreo(string correo);

    DateTime? UltimoCorreoUtc();

    void MarcarCorreoEnviado(DateTime instante);
}
