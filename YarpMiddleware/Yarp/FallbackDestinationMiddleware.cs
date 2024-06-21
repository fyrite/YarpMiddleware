using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NLog;
using YarpMiddleware.Extensions;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Model;

namespace YarpMiddleware.Yarp
{
    /// <summary>
    /// Invokes the proxy on a healthy destination if the original request failed.
    /// </summary>
    public class FallbackDestinationMiddleware
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        private readonly IDestinationHealthUpdater healthUpdater;
        private readonly RequestDelegate next;
        private readonly IHttpForwarder httpForwarder;
        private readonly ILoadBalancer loadBalancer;
    
        public FallbackDestinationMiddleware(RequestDelegate next,
            IDestinationHealthUpdater healthUpdater,
            IHttpForwarder httpForwarder,
            ILoadBalancer loadBalancer)
        {
            this.next = next;
            this.healthUpdater = healthUpdater;
            this.httpForwarder = httpForwarder;
            this.loadBalancer = loadBalancer;
        }
    
        public async Task Invoke(HttpContext context)
        {
            // Copy the original response body stream as we will need this if we need to resubmit the request.
            var originalBodyStream = context.Response.Body;
            
            // Make the request.
            await next(context);
        
            if (!context.DetermineIfDestinationFailed())
            {
                return; 
            }
        
            // Get the current healthy destinations from the ReverseProxyFeature.
            var proxyFeature = context.GetReverseProxyFeature();
            var healthyDestinations = proxyFeature
                .AllDestinations
                .Where(d => d.Health.Passive != DestinationHealth.Unhealthy)
                .ToList();
            
            // Set these variables outside the loop in order to reduce redundancy. 
            var route = context.GetRouteModel();
            var clusterModel = proxyFeature.Cluster;
            var config = clusterModel.Config;

            // Assume we have an error and proceed with making requests to the next healthy destination.
            var hasError = true;
            while (healthyDestinations.Count > 0 && hasError)
            {
                // Exit if the response headers have already been sent to the client.
                // This error can occur at times and we try to circumvent the issue by copying the original
                // response body stream up front.
                if (context.Response.HasStarted)
                {
                    break;
                }
            
                // Reset these properties in order to be able to make another proxied request with the same context.
                context.Response.Body = originalBodyStream;
                context.Response.StatusCode = (int) HttpStatusCode.OK;
                context.Features.Set<IForwarderErrorFeature>(null);
            
                // Pick the next healthy destination using the configure / default load balancing policy.
                var destination = loadBalancer.PickDestination(healthyDestinations, context, route, config);

                try
                {
                    // Send on the request.
                    var result = await httpForwarder.SendAsync(
                        context, 
                        destination.Model.Config.Address, 
                        clusterModel.HttpClient,
                        clusterModel.Config.HttpRequest ?? ForwarderRequestConfig.Empty, 
                        route.Transformer);

                    // If the request failed, log a warning and retry if possible to the next healthy destination.
                    hasError = context.DetermineIfDestinationFailed();
                    if (hasError)
                    {
                        Log.Warn($"Proxying to fallback destination {destination.DestinationId} failed " +
                                 $"with ForwarderError.'{result}'.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"An error occured proxying to fallback destination {destination.DestinationId}.");
                    hasError = true;
                }
                finally
                {
                    if (hasError)
                    {
                        // If the request failed, remove the destination from the healthy destinations collection and
                        // flag the destination as unhealthy.
                        healthyDestinations.Remove(destination);
                        healthUpdater.FlagAsUnhealthy(route.Cluster!, destination);
                    }
                }
            }
        }
    }
}