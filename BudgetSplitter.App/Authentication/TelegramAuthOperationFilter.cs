using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BudgetSplitter.App.Authentication;

public sealed class TelegramAuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true)
                                   ?? [];
        var attributes = endpointAttributes.Concat(controllerAttributes).ToArray();

        if (attributes.OfType<IAllowAnonymous>().Any() || !attributes.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "TelegramInitData"
                }
            }] = Array.Empty<string>()
        });
    }
}
