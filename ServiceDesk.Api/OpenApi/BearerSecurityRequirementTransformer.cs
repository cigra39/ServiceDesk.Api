using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ServiceDesk.Api.OpenApi
{
    public sealed class BearerSecurityRequirementTransformer
        : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var endpointMetadata =
                context.Description.ActionDescriptor.EndpointMetadata;

            var requiresAuthorization =
                endpointMetadata.OfType<IAuthorizeData>().Any();

            var allowsAnonymousAccess =
                endpointMetadata.OfType<IAllowAnonymous>().Any();

            if (!requiresAuthorization || allowsAnonymousAccess)
            {
                return Task.CompletedTask;
            }

            operation.Security ??= [];

            operation.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "Bearer",
                            context.Document)
                    ] = []
                });

            return Task.CompletedTask;
        }
    }
}
