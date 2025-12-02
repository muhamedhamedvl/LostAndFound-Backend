using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;
namespace LostAndFound.Api.Filters
{
    public class FileUploadParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            try
            {
                var parameterInfo = context.ParameterInfo;
                
                if (parameterInfo != null)
                {
                    var hasFromForm = parameterInfo.GetCustomAttributes(typeof(FromFormAttribute), false).Any();
                    var isFormFile = parameterInfo.ParameterType == typeof(IFormFile) || 
                                    parameterInfo.ParameterType == typeof(IFormFile[]);
                    
                    if (hasFromForm && isFormFile)
                    {
                        parameter.Name = null;
                        parameter.In = null;
                        parameter.Schema = null;
                        parameter.Description = "IGNORE_THIS_PARAMETER";
                        parameter.Required = false;
                    }
                }
            }
            catch
            {
            }
        }
    }
}

