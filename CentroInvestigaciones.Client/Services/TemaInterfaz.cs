using Microsoft.JSInterop;

namespace CentroInvestigaciones.Client.Services;

public sealed record OpcionTema(
    string Codigo,
    string Nombre,
    string FondoVista,
    string Color1,
    string Color2,
    string Color3);

public sealed class TemaInterfaz
{
    public static IReadOnlyList<OpcionTema> Catalogo { get; } =
    [
        new("claro", "Claro", "#f5f9fd", "#5b92c9", "#8eafd0", "#e2b56a"),
        new("oscuro", "Oscuro", "#2a3340", "#7eafdf", "#a8b8c8", "#d9a060"),
        new("slate", "Pizarra", "#eef2f6", "#7d8c9e", "#5a6b7c", "#b7c3d1"),
        new("teal", "Turquesa", "#e8f7f5", "#4eb0a4", "#78c8c0", "#a8dcd4"),
        new("oceano", "Océano", "#e8f4fb", "#5aabc8", "#7ab8d4", "#a3d2e6"),
        new("forest", "Bosque", "#eef6f0", "#6aaf80", "#8ec4a0", "#b4dcc0"),
        new("atardecer", "Atardecer", "#fff5eb", "#e09068", "#d4a078", "#f0c890"),
        new("rosa", "Rosa", "#fdf2f7", "#d48eac", "#e0a8c4", "#f0bdd4"),
        new("indigo", "Índigo", "#f0f2ff", "#8e9ae4", "#a8b0e8", "#b8c4f0"),
        new("lavanda", "Lavanda", "#f6f3fb", "#a892cc", "#bfb0dc", "#d4c8ea"),
        new("cyberpunk", "Ciberpunk", "#1a1528", "#b070ee", "#50d4e8", "#e890c4")
    ];

    private readonly IJSRuntime _js;

    public TemaInterfaz(IJSRuntime js)
    {
        _js = js;
    }

    public string Actual { get; private set; } = "claro";

    public event Action? Cambio;

    public async Task CargarAsync(string? preferido = null)
    {
        var desdeNavegador = await _js.InvokeAsync<string>("centroTema.leer");
        Actual = Normalizar(preferido ?? desdeNavegador);
        await _js.InvokeVoidAsync("centroTema.aplicar", Actual);
        Cambio?.Invoke();
    }

    public async Task AplicarAsync(string codigo)
    {
        Actual = Normalizar(codigo);
        await _js.InvokeVoidAsync("centroTema.aplicar", Actual);
        Cambio?.Invoke();
    }

    public static string Normalizar(string? codigo)
    {
        var valor = string.IsNullOrWhiteSpace(codigo) ? "claro" : codigo.Trim().ToLowerInvariant();
        return Catalogo.Any(item => item.Codigo == valor) ? valor : "claro";
    }
}
