using CentroInvestigaciones.Domain;
using CentroInvestigaciones.Shared;

namespace CentroInvestigaciones.API.Infraestructura;

public static class MapaProtocolo
{
    public static Protocolo Desde(GuardarProtocoloDto solicitud) => new()
    {
        Formato = solicitud.Formato,
        Nombre = solicitud.Nombre,
        Correo = solicitud.Correo,
        RevisorId = solicitud.RevisorId,
        Respuestas = solicitud.Respuestas,
        Conocimiento = solicitud.Conocimiento.Select(Copiar).ToList(),
        Tecnologico = solicitud.Tecnologico.Select(Copiar).ToList(),
        Capacidad = solicitud.Capacidad.Select(Copiar).ToList(),
        CronogramaFoscal = solicitud.CronogramaFoscal.Select(Copiar).ToList(),
        CronogramaUdes = solicitud.CronogramaUdes.Select(Copiar).ToList(),
        Presupuesto = solicitud.Presupuesto.Select(fila => new FilaPresupuestoProtocolo
        {
            Rubro = fila.Rubro,
            Valores = fila.Valores
        }).ToList(),
        EticaPoblacion = solicitud.EticaPoblacion,
        EticaIntervencion = solicitud.EticaIntervencion,
        EticaInstrumentos = solicitud.EticaInstrumentos
    };

    public static ProtocoloDto Hacia(Protocolo protocolo, string revisorNombre) => new()
    {
        Id = protocolo.Id,
        Token = protocolo.Token,
        Estado = protocolo.Estado,
        Formato = protocolo.Formato,
        Nombre = protocolo.Nombre,
        Correo = protocolo.Correo,
        RevisorId = protocolo.RevisorId,
        RevisorNombre = revisorNombre,
        Respuestas = new Dictionary<string, string>(protocolo.Respuestas, StringComparer.Ordinal),
        Comentarios = new Dictionary<string, string>(protocolo.Comentarios, StringComparer.Ordinal),
        Conocimiento = protocolo.Conocimiento.Select(fila => new FilaConocimientoProtocoloDto
        {
            Resultado = fila.Resultado,
            Indicador = fila.Indicador,
            Beneficiario = fila.Beneficiario
        }).ToList(),
        Tecnologico = protocolo.Tecnologico.Select(fila => new FilaProductoProtocoloDto
        {
            Objetivo = fila.Objetivo,
            Resultado = fila.Resultado,
            Indicador = fila.Indicador,
            Beneficiario = fila.Beneficiario
        }).ToList(),
        Capacidad = protocolo.Capacidad.Select(fila => new FilaProductoProtocoloDto
        {
            Objetivo = fila.Objetivo,
            Resultado = fila.Resultado,
            Indicador = fila.Indicador,
            Beneficiario = fila.Beneficiario
        }).ToList(),
        CronogramaFoscal = protocolo.CronogramaFoscal.Select(Hacia).ToList(),
        CronogramaUdes = protocolo.CronogramaUdes.Select(Hacia).ToList(),
        Presupuesto = Presupuesto(protocolo),
        EticaPoblacion = Marcas(protocolo.EticaPoblacion, CatalogoProtocolo.EticaPoblacion.Length),
        EticaIntervencion = Marcas(protocolo.EticaIntervencion, CatalogoProtocolo.EticaIntervencion.Length),
        EticaInstrumentos = Marcas(protocolo.EticaInstrumentos, CatalogoProtocolo.EticaInstrumentos.Length),
        CreadoEl = protocolo.CreadoEl,
        EditadoEl = protocolo.EditadoEl
    };

    public static GuardarProtocoloDto Guardar(ProtocoloDto protocolo) => new()
    {
        Formato = protocolo.Formato,
        Nombre = protocolo.Nombre,
        Correo = protocolo.Correo,
        RevisorId = protocolo.RevisorId,
        Respuestas = protocolo.Respuestas,
        Conocimiento = protocolo.Conocimiento,
        Tecnologico = protocolo.Tecnologico,
        Capacidad = protocolo.Capacidad,
        CronogramaFoscal = protocolo.CronogramaFoscal,
        CronogramaUdes = protocolo.CronogramaUdes,
        Presupuesto = protocolo.Presupuesto,
        EticaPoblacion = protocolo.EticaPoblacion,
        EticaIntervencion = protocolo.EticaIntervencion,
        EticaInstrumentos = protocolo.EticaInstrumentos
    };

    private static FilaConocimientoProtocolo Copiar(FilaConocimientoProtocoloDto fila) => new()
    {
        Resultado = fila.Resultado,
        Indicador = fila.Indicador,
        Beneficiario = fila.Beneficiario
    };

    private static FilaProductoProtocolo Copiar(FilaProductoProtocoloDto fila) => new()
    {
        Objetivo = fila.Objetivo,
        Resultado = fila.Resultado,
        Indicador = fila.Indicador,
        Beneficiario = fila.Beneficiario
    };

    private static FilaCronogramaProtocolo Copiar(FilaCronogramaProtocoloDto fila) => new()
    {
        Actividad = fila.Actividad,
        Responsable = fila.Responsable,
        Meses = fila.Meses
    };

    private static FilaCronogramaProtocoloDto Hacia(FilaCronogramaProtocolo fila) => new()
    {
        Actividad = fila.Actividad,
        Responsable = fila.Responsable,
        Meses = Marcas(fila.Meses, 12)
    };

    private static List<FilaPresupuestoProtocoloDto> Presupuesto(Protocolo protocolo)
    {
        var filas = new List<FilaPresupuestoProtocoloDto>();
        for (var indice = 0; indice < CatalogoProtocolo.Rubros.Length; indice++)
        {
            var origen = indice < protocolo.Presupuesto.Count ? protocolo.Presupuesto[indice].Valores : [];
            var valores = new List<string>();
            for (var columna = 0; columna < CatalogoProtocolo.Fuentes.Length; columna++)
            {
                valores.Add(columna < origen.Count ? origen[columna] : "");
            }

            filas.Add(new FilaPresupuestoProtocoloDto
            {
                Rubro = CatalogoProtocolo.Rubros[indice],
                Valores = valores
            });
        }

        return filas;
    }

    private static List<bool> Marcas(IReadOnlyList<bool> origen, int cantidad)
    {
        var marcas = new List<bool>(cantidad);
        for (var indice = 0; indice < cantidad; indice++)
        {
            marcas.Add(indice < origen.Count && origen[indice]);
        }

        return marcas;
    }
}
