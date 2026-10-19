using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.Client.Services;

public sealed class EstadoSesion
{
    public SesionActualDto Actual { get; private set; } = new();

    public bool Cargando { get; private set; } = true;

    public event Action? Cambio;

    public async Task CargarAsync(ClienteSesion cliente)
    {
        Cargando = true;
        Notificar();

        try
        {
            Actual = await cliente.ObtenerActualAsync();
        }
        catch (HttpRequestException)
        {
            Actual = new SesionActualDto();
        }
        finally
        {
            Cargando = false;
            Notificar();
        }
    }

    private void Notificar() => Cambio?.Invoke();
}
