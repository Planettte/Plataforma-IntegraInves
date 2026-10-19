namespace CentroInvestigaciones.API.Autorizacion;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequierePermisoAttribute : Attribute
{
    public RequierePermisoAttribute(string codigo)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}
