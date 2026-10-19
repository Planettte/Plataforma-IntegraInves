using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CentroInvestigaciones.Data;

public static class DependenciasDatos
{
    public static IServiceCollection AgregarDatos(this IServiceCollection servicios, string conexion)
    {
        servicios.AddDbContext<CentroDbContext>(opciones => opciones.UseSqlServer(conexion));
        return servicios;
    }
}
