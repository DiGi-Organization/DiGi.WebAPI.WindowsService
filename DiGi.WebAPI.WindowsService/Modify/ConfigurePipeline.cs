using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace DiGi.WebAPI.WindowsService.Modify
{
    public static partial class Modify
    {
        /// <summary>
        /// Adds the host's request pipeline to the specified web application: the Swagger rewrite and documents, exception and status code pages, HTTPS redirection, CORS, request decompression, authorization when an authorization service is registered, and the controllers.
        /// <para>Kept outside <see cref="Program"/> so that a fact can run a request through exactly the pipeline the service runs (DiGi-Organization/DiGi.GIS.WebAPI#77).</para>
        /// </summary>
        /// <param name="webApplication">The web application whose pipeline is configured. Nothing is done when it is <c>null</c>.</param>
        public static void ConfigurePipeline(this WebApplication? webApplication)
        {
            if (webApplication is null)
            {
                return;
            }

            // /swagger/swagger.json is the short address of the full document. Rewritten internally (no redirect)
            // because Swashbuckle's route template must contain {documentName}.
            webApplication.UseRewriter(new RewriteOptions().AddRewrite(@"^swagger/swagger\.json$", $"swagger/{Constants.Name.SwaggerDocument_Full}/swagger.json", skipRemainingRules: true));

            webApplication.UseSwagger();
            Serilog.Modify.Log("Swagger in use");

            if (webApplication.Environment.IsDevelopment())
            {
                List<string> documentNames = webApplication.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().DocumentNames();

                webApplication.UseSwaggerUI(swaggerUIOptions =>
                {
                    swaggerUIOptions.SwaggerEndpoint("/swagger/swagger.json", "Full");

                    foreach (string documentName in documentNames)
                    {
                        swaggerUIOptions.SwaggerEndpoint($"/swagger/{documentName}/swagger.json", documentName);
                    }
                });
                Serilog.Modify.Log("Swagger UI in use");
            }

            webApplication.UseExceptionHandler();
            webApplication.UseStatusCodePages();

            webApplication.UseHttpsRedirection();
            webApplication.UseCors(Constants.Name.Policy);
            webApplication.UseRequestDecompression();

            Microsoft.AspNetCore.Authorization.IAuthorizationService? authorizationService = webApplication.Services.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
            if (authorizationService != null)
            {
                Serilog.Modify.Log("Authorization in use");
                webApplication.UseAuthorization();
            }
            else
            {
                Serilog.Modify.Log("Authorization not in use");
            }

            webApplication.MapControllers();
        }
    }
}
