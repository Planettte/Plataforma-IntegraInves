namespace CentroInvestigaciones.API.Infraestructura;

public sealed class VigilanciaDispositivos : BackgroundService
{
    private readonly AvisadorDispositivos _avisador;
    private readonly ILogger<VigilanciaDispositivos> _registro;

    public VigilanciaDispositivos(AvisadorDispositivos avisador, ILogger<VigilanciaDispositivos> registro)
    {
        _avisador = avisador;
        _registro = registro;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await Esperar(TimeSpan.FromSeconds(12), stoppingToken))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _avisador.RevisarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception excepcion)
            {
                _registro.LogWarning(excepcion, "No se pudo revisar el semáforo de dispositivos.");
            }

            if (!await Esperar(TimeSpan.FromDays(1), stoppingToken))
            {
                return;
            }
        }
    }

    private static async Task<bool> Esperar(TimeSpan espera, CancellationToken ct)
    {
        try
        {
            await Task.Delay(espera, ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
