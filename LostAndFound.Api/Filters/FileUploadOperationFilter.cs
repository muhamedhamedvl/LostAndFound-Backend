using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
namespace LostAndFound.Api.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile) ||
                           p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile[]))
                .ToList();

            if (fileParameters.Any())
            {
                var requiredParams = fileParameters
                    .Where(p => !p.HasDefaultValue)
                    .Select(p => p.Name!)
                    .ToHashSet();

                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = fileParameters.ToDictionary(
                                    p => p.Name!,
                                    p => new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    }
                                ),
                                Required = requiredParams
                            }
                        }
                    }
                };
                var parametersToRemove = operation.Parameters
                    .Where(p => fileParameters.Any(fp => fp.Name == p.Name) || 
                               p.Description == "IGNORE_THIS_PARAMETER")
                    .ToList();
                
                foreach (var paramToRemove in parametersToRemove)
                {
                    operation.Parameters.Remove(paramToRemove);
                }

                var formParameters = context.MethodInfo.GetParameters()
                    .Where(p => p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromFormAttribute), false).Any() &&
                               p.ParameterType != typeof(Microsoft.AspNetCore.Http.IFormFile) &&
                               p.ParameterType != typeof(Microsoft.AspNetCore.Http.IFormFile[]))
                    .ToList();

                if (formParameters.Any() && operation.RequestBody?.Content != null)
                {
                    var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
                    if (schema.Required == null)
                    {
                        schema.Required = new HashSet<string>();
                    }

                    foreach (var param in formParameters)
                    {
                        var paramSchema = new OpenApiSchema();
                        
                        if (param.ParameterType == typeof(int) || param.ParameterType == typeof(int?))
                        {
                            paramSchema.Type = "integer";
                            paramSchema.Format = "int32";
                        }
                        else if (param.ParameterType == typeof(string))
                        {
                            paramSchema.Type = "string";
                        }
                        else if (param.ParameterType == typeof(bool) || param.ParameterType == typeof(bool?))
                        {
                            paramSchema.Type = "boolean";
                        }
                        else
                        {
                            paramSchema.Type = "string";
                        }

                        schema.Properties[param.Name!] = paramSchema;
                        
                        if (!param.HasDefaultValue)
                        {
                            schema.Required.Add(param.Name!);
                        }
                    }
                    foreach (var param in formParameters)
                    {
                        var parameterToRemove = operation.Parameters
                            .FirstOrDefault(p => p.Name == param.Name);
                        if (parameterToRemove != null)
                        {
                            operation.Parameters.Remove(parameterToRemove);
                        }
                    }
                }
            }
        }
    }
}

