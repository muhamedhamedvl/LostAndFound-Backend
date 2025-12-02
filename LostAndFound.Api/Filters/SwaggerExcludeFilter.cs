using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
namespace LostAndFound.Api.Filters
{
    public class SwaggerExcludeFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            try
            {
                if (schema == null || context == null || context.Type == null)
                    return;

                var typeName = context.Type.Name;
                
                if (typeName == "CategoryDto" && schema.Properties != null)
                {
                    if (schema.Properties.ContainsKey("childCategories"))
                    {
                        schema.Properties.Remove("childCategories");
                    }
                }
            }
            catch
            {
            }
        }
    }
}

