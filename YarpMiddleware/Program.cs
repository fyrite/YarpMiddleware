using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using Yarp.ReverseProxy.Health;
using YarpMiddleware.Yarp;
using ILogger = NLog.ILogger;

namespace YarpMiddleware
{
    internal static class Program
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            var app = ConfigureBuilder(args).Build();
            
            ConfigureYarp(app);

            app.MapHealthChecks("/_health");
            app.Run();
        }

        private static WebApplicationBuilder ConfigureBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;
            
            builder.WebHost
                .ConfigureKestrel((context, options) =>
                {
                    options.Configure(config.GetSection("Kestrel"));
                });

            builder.Logging
                .ClearProviders()
                .AddNLogWeb();
            
            builder.Services
                .AddHealthChecks();

            ConfigureYarp(builder.Services, config);
            
            builder.Host
                .UseNLog()
                .UseWindowsService();

            return builder;
        }

        private static void ConfigureYarp(IEndpointRouteBuilder app)
        {
            app.MapReverseProxy(proxyPipeline =>
            {
                proxyPipeline.UseLoadBalancing();
                proxyPipeline.UseMiddleware<FallbackDestinationMiddleware>();
                proxyPipeline.UsePassiveHealthChecks();
            });
        }

        private static void ConfigureYarp(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<ILoadBalancer, FallbackDestinationLoadBalancer>();
            services.AddSingleton<IPassiveHealthCheckPolicy, FirstFailedResponseHealthPolicy>();

            services
                .AddReverseProxy()
                .LoadFromConfig(config.GetSection("ReverseProxy"));
        }
        
        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Log.Error(exception, $"UNHANDLED EXCEPTION!!! IsTerminating={e.IsTerminating}");
            }
        }
    }
}