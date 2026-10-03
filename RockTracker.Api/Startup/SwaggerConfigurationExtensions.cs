using Microsoft.OpenApi.Models;
using RockTracker.Api.Common;

namespace RockTracker.Api.Startup;

public static class SwaggerConfigurationExtensions
{
    public static void AddApiKeySwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(ApiKeyConstants.HeaderName, new OpenApiSecurityScheme
            {
                Name = ApiKeyConstants.HeaderName,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "API key authentication. Enter your API key below."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = ApiKeyConstants.HeaderName
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
}
