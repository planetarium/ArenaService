using Libplanet.Types.Tx;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class TxIdSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(TxId))
        {
            schema.Type = "string";
            schema.Format = "hex";
        }
    }
}
