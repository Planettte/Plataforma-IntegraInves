using CentroInvestigaciones.Domain;

namespace CentroInvestigaciones.Application.Dispositivos;

public interface IAlmacenDispositivos
{
    IReadOnlyList<DispositivoMedico> Listar();

    DispositivoMedico? Buscar(Guid id);

    DispositivoMedico Crear(DispositivoMedico datos, string persona);

    DispositivoMedico? Actualizar(Guid id, DispositivoMedico datos, string persona);

    IReadOnlyList<string> Avisos();

    void ReemplazarAvisos(IReadOnlyList<string> claves);

    string CorreoEncargado();

    void GuardarCorreo(string correo);

    DateTime? UltimoCorreoUtc();

    void MarcarCorreoEnviado(DateTime instante);
}
