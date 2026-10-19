namespace CentroInvestigaciones.Client.Services;

public sealed class Avisos
{
    private int _serie;

    public event Action? Cambio;

    public bool Visible { get; private set; }

    public void Completado()
    {
        Visible = true;
        var serie = ++_serie;
        Cambio?.Invoke();
        _ = OcultarAsync(serie);
    }

    private async Task OcultarAsync(int serie)
    {
        await Task.Delay(2200);
        if (serie != _serie)
        {
            return;
        }

        Visible = false;
        Cambio?.Invoke();
    }
}
