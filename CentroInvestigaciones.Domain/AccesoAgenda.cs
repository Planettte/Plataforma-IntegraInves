namespace CentroInvestigaciones.Domain;

public static class AccesoAgenda
{
    public const string PermisoEstudios = "herramientas.estudios.agenda";
    public const string PermisoUdd = "herramientas.udd.agenda";
    public const string AreaEstudios = "estudios";
    public const string AreaUdd = "udd";
    public const string AreaComite = "comite";
    public const string Coordinacion = "coordinacion";
    public const string Medico = "medico";
    public const string Apoyo = "apoyo";
    public const string Farmacia = "farmacia";
    public const string CargoUdd = "udd";
    public const string CargoUsuario = "usuario";

    public static readonly string[] CargosEstudios = [Coordinacion, Medico, Apoyo, Farmacia];

    public static readonly string[] Permisos = [PermisoEstudios, PermisoUdd];

    public static bool CargoEstudiosValido(string? cargo) =>
        CargosEstudios.Contains(Normalizar(cargo), StringComparer.Ordinal);

    public static bool RequiereElegirCargo(string? codigoArea, string? cargo) =>
        string.Equals(Normalizar(codigoArea), AreaEstudios, StringComparison.Ordinal)
        && !CargoEstudiosValido(cargo);

    public static string CargoAlAsignarArea(string? codigoArea, string? cargoActual)
    {
        var area = Normalizar(codigoArea);
        if (area == AreaUdd)
        {
            return CargoUdd;
        }

        if (area == AreaComite)
        {
            return CargoUsuario;
        }

        if (area == AreaEstudios)
        {
            return CargoEstudiosValido(cargoActual) ? Normalizar(cargoActual) : "";
        }

        return cargoActual?.Trim() ?? "";
    }

    public static string? PermisoPorCuenta(string? codigoArea, string? cargo)
    {
        var area = Normalizar(codigoArea);
        var valor = Normalizar(cargo);
        if (area == AreaEstudios && CargosEstudios.Contains(valor, StringComparer.Ordinal))
        {
            return PermisoEstudios;
        }

        if (area == AreaUdd)
        {
            return PermisoUdd;
        }

        return null;
    }

    public static string Etiqueta(string? cargo) => Normalizar(cargo) switch
    {
        Coordinacion => "Coordinación",
        Medico => "Médico",
        Apoyo => "Apoyo",
        Farmacia => "Farmacia",
        CargoUdd => "UDD",
        CargoUsuario => "Usuario",
        _ => string.IsNullOrWhiteSpace(cargo) ? "Sin indicar" : cargo.Trim()
    };

    public static string Normalizar(string? valor) => valor?.Trim().ToLowerInvariant() ?? "";
}
