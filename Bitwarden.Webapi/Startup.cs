﻿using System.Globalization;
using System.Collections.Generic;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using AutoMapper;

using Bit.Notifications;
using Bit.Core;
using Bit.Core.Identity;
using Bit.Core.Utilities;



namespace Bit.Api
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; private set; }
        public IWebHostEnvironment Environment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            
            // Options
            services.AddOptions();

            // Settings
            var globalSettings = services.AddGlobalSettingsServices(Configuration);

            // Data Protection
            services.AddCustomDataProtectionServices(Environment, globalSettings);

            // Repositories
            //services.AddSqlServerRepositories(globalSettings);
            services.AddSqlServerRepositories(globalSettings);
            services.AddAutoMapper(typeof(Startup));

            // Context            

            // Caching
            services.AddMemoryCache();

            
            
            services.AddCustomCookiePolicy();
            services.AddCustomSingleSignOn(globalSettings);
            // Identity
            services.AddCustomIdentityServices(globalSettings);
            services.AddIdentityAuthenticationServices(globalSettings, Environment);
            services.AddCustomAuthorizationPolicy();

            services.AddScoped<AuthenticatorTokenProvider>();

            // IdentityServer
            services.AddCustomIdentityServerServices(Environment, globalSettings);

            // Identity
            services.AddCustomIdentityServices(globalSettings);
            
            services.AddServices();           

            services.AddCoreLocalizationServices();

            // MVC
            /*
            services.AddMvc(config =>
            {
                config.Conventions.Add(new ApiExplorerGroupConvention());
                config.Conventions.Add(new PublicApiControllersModelConvention());
            }).AddNewtonsoftJson(options =>
            {
                if (Environment.IsProduction() && Configuration["swaggerGen"] != "true")
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                }
            });
            */
            services.AddControllers().AddNewtonsoftJson();
            services.AddScoped<ISessionContext,SessionContext >();
            services.AddHttpContextAccessor();

            //services.AddSwagger(globalSettings);
            services.AddSwaggerGen();
            services.AddCustomSignalR();
            //services.AddSwaggerGenNewtonsoftSupport();

            //Jobs.JobsHostedService.AddJobsServices(services);
            //services.AddHostedService<Jobs.JobsHostedService>();

            if (globalSettings.SelfHosted)
            {
                // Jobs service
                //Jobs.JobsHostedService.AddJobsServices(services);
                //services.AddHostedService<Jobs.JobsHostedService>();
            }
            if (CoreHelpers.SettingHasValue(globalSettings.ServiceBus.ConnectionString) &&
                CoreHelpers.SettingHasValue(globalSettings.ServiceBus.ApplicationCacheTopicName))
            {
                services.AddHostedService<Core.HostedServices.ApplicationCacheHostedService>();
            }
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IHostApplicationLifetime appLifetime,
            GlobalSettings globalSettings,
            ILogger<Startup> logger)
        {
            IdentityModelEventSource.ShowPII = true;
            app.UseSerilog(env, appLifetime, globalSettings);

            // Default Middleware
            app.UseDefaultMiddleware(env, globalSettings);

            app.UseForwardedHeaders(globalSettings);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCookiePolicy();
            }            

            // Add localization
            app.UseCoreLocalization();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });                 

            // Add routing
            app.UseRouting();

            // Add Cors
            app.UseCors(policy => policy.SetIsOriginAllowed(o => CoreHelpers.IsCorsOriginAllowed(o, globalSettings))
                .AllowAnyMethod().AllowAnyHeader().AllowCredentials());

            // Add authentication and authorization to the request pipeline.
            app.UseAuthentication();
            app.UseAuthorization();

            // Add current context
            //app.UseMiddleware<SessionContextMiddleware>();

            // Add IdentityServer to the request pipeline.
            app.UseIdentityServer();

            // Add endpoints to the request pipeline.
            app.UseEndpoints(endpoints =>
            {
                
                endpoints.MapHub<Bit.Notifications.NotificationsHub>("/hub", options =>
                {
                    options.ApplicationMaxBufferSize = 2048; // client => server messages are not even used
                    options.TransportMaxBufferSize = 4096;
                });
                
                endpoints.MapDefaultControllerRoute();
            });                        
            // Add Swagger
            /*
            if (Environment.IsDevelopment() || globalSettings.SelfHosted)
            {
                app.UseSwagger(config =>
                {
                    config.RouteTemplate = "specs/{documentName}/swagger.json";
                    config.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                        swaggerDoc.Servers = new List<OpenApiServer>
                        {
                            new OpenApiServer { Url = globalSettings.BaseServiceUri.Api }
                        });
                });
                app.UseSwaggerUI(config =>
                {
                    config.DocumentTitle = "Bitwarden API Documentation";
                    config.RoutePrefix = "docs";
                    config.SwaggerEndpoint($"{globalSettings.BaseServiceUri.Api}/specs/public/swagger.json",
                        "Bitwarden Public API");
                    config.OAuthClientId("accountType.id");
                    config.OAuthClientSecret("secretKey");
                });
            }
            */
       
            app.RunProxy(new ProxyOptions(){Host="localhost",Port="8080", Scheme="http"});

            // Log startup
            logger.LogInformation(Constants.BypassFiltersEventId, globalSettings.ProjectName + " started.");
        }
    }
}
