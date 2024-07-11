using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProductApi.Services;
using System;
using System.Net.Http;

namespace ProductApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<IProductsService, ProductsService>();

            var keycloakAuthority = Configuration["Keycloak:Authority"];
            var keycloakClientId = Configuration["Keycloak:ClientId"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakAuthority;
                options.Audience = keycloakClientId;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = keycloakAuthority,
                    ValidAudience = keycloakClientId,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var client = new HttpClient();
                        var keySetUrl = $"{keycloakAuthority}/protocol/openid-connect/certs";
                        var response = client.GetAsync(keySetUrl).Result;
                        var keys = response.Content.ReadAsStringAsync().Result;
                        var jsonWebKeySet = new JsonWebKeySet(keys);
                        return jsonWebKeySet.Keys;
                    }
                };
            });

            services.AddHealthChecks()
                .AddCheck("random-health-status", () =>
                {
                    Random rnd = new();
                    var rndNum = rnd.Next();
                    for (int j = 0; j < 100; j++)
                    {
                        if (rndNum % 2 == 0)
                        {
                            return HealthCheckResult.Unhealthy();
                        }
                        else
                        {
                            return HealthCheckResult.Healthy();
                        }
                    }
                    return HealthCheckResult.Unhealthy();
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/api/health");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
