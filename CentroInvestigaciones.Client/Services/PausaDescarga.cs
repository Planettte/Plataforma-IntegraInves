namespace CentroInvestigaciones.Client.Services;

public static class PausaDescarga
{
    public static async Task CompletarAsync(DateTime inicio)
    {
        var espera = TimeSpan.FromSeconds(5) - (DateTime.UtcNow - inicio);
        if (espera > TimeSpan.Zero)
        {
            await Task.Delay(espera);
        }
    }
}
