using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ServiceDesk.Api.OpenApi
{
    public sealed class BearerSecuritySchemeTransformer
        : IOpenApiDocumentTransformer
    {
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        public BearerSecuritySchemeTransformer(
            IAuthenticationSchemeProvider authenticationSchemeProvider)
        {
            _authenticationSchemeProvider = authenticationSchemeProvider;
        }

        public async Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authenticationSchemes =
                await _authenticationSchemeProvider.GetAllSchemesAsync();

            var bearerIsRegistered = authenticationSchemes.Any(
                authenticaitonScheme =>
                   authenticaitonScheme.Name ==
                   JwtBearerDefaults.AuthenticationScheme);

            if (!bearerIsRegistered)
            {
                return;
            }

            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes =
                new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    }
                };
        }
    }
}