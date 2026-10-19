namespace CentroInvestigaciones.Shared;

public sealed class FormatoFarmaciaDto
{
    public Guid Id { get; set; }

    public string NumeroEstudio { get; set; } = "";

    public string Patrocinador { get; set; } = "";

    public string Coordinador { get; set; } = "";

    public string NumeroCentro { get; set; } = "";

    public string Titulo { get; set; } = "";

    public string InvestigadorPrincipal { get; set; } = "";

    public string CondicionesAlmacenamiento { get; set; } = "";

    public string DescripcionMedicamento { get; set; } = "";

    public string Dosis { get; set; } = "";

    public string Observaciones { get; set; } = "";

    public string ResponsableFarmacia { get; set; } = "";

    public string FirmaCierre { get; set; } = "";

    public string CantidadFinal { get; set; } = "";

    public DateTimeOffset? GeneradoEl { get; set; }

    public List<FilaFarmaciaDto> Filas { get; set; } = [];
}

public sealed class FilaFarmaciaDto
{
    public Guid Id { get; set; }

    public string NumeroEnvio { get; set; } = "";

    public string FechaRecibido { get; set; } = "";

    public string Lote { get; set; } = "";

    public string FechaVencimiento { get; set; } = "";

    public string CodigoMedicamento { get; set; } = "";

    public string Conforme { get; set; } = "";

    public string SujetoCodigo { get; set; } = "";

    public string FechaEntrega { get; set; } = "";

    public string Entrega { get; set; } = "";

    public string Recibe { get; set; } = "";

    public string FechaRetorno { get; set; } = "";

    public string Empaque { get; set; } = "";

    public string Unidades { get; set; } = "";

    public string EntregaRetorno { get; set; } = "";

    public string RecibeRetorno { get; set; } = "";

    public string ControlMonitoria { get; set; } = "";

    public string FechaDisposicion { get; set; } = "";

    public string RetornoPatrocinador { get; set; } = "";

    public string DestruccionActa { get; set; } = "";
}
