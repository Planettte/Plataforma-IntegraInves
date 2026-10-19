namespace CentroInvestigaciones.Data;

public static class SondaSql
{
    public static async Task<bool> RespondeAsync(string conexion, CancellationToken ct = default)
    {
        try
        {
            var texto = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(conexion)
            {
                ConnectTimeout = 2
            }.ConnectionString;
            await using var sql = new Microsoft.Data.SqlClient.SqlConnection(texto);
            await sql.OpenAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
