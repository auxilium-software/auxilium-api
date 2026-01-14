using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AuxiliumSoftware.AuxiliumServices.API.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // grab all the parameters that are file uploads (IFormFile)
            var fileParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => p.ModelMetadata != null &&
                            p.ModelMetadata.ModelType == typeof(IFormFile)
                       )
                .ToList();

            // if there are no file parameters, we don't need to do anything
            if (!fileParameters.Any())
                return;

            // clear default parameters as we're gonna be using the request body instead
            operation.Parameters?.Clear();

            // build the schema properties for the multipart form
            var properties = new Dictionary<string, OpenApiSchema>();
            var required = new HashSet<string>();

            // go through all of the parameters to build the form schema
            foreach (var param in context.ApiDescription.ParameterDescriptions)
            {
                // file parameters use binary format
                if (param.ModelMetadata?.ModelType == typeof(IFormFile))
                {
                    properties[param.Name] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    };
                }
                // everything else should be treated as string fields
                else
                {
                    properties[param.Name] = new OpenApiSchema
                    {
                        Type = "string"
                    };
                }

                // all parameters are required
                required.Add(param.Name);
            }

            // set the operation to use multipart/form-data request body with the built schema
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties,
                            Required = required
                        }
                    }
                }
            };
        }
    }
}
