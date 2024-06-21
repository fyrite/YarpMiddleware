using Microsoft.AspNetCore.Http;
using YarpMiddleware.Extensions;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Model;

namespace YarpMiddleware.Yarp
{
    /// <summary>
    /// Health policy that marks the destination as Unhealthy on the first failed response to the proxied request. 
    /// </summary>
    public class FirstFailedResponseHealthPolicy: IPassiveHealthCheckPolicy
    {
        private readonly IDestinationHealthUpdater healthUpdater;
    
        public FirstFailedResponseHealthPolicy(IDestinationHealthUpdater healthUpdater)
        {
            this.healthUpdater = healthUpdater;
        }
    
        public string Name => "FirstFailedResponse";
    
        public void RequestProxied(HttpContext context, ClusterState cluster, DestinationState destination)
        {
            if (context.DetermineIfDestinationFailed())
            {
                healthUpdater.FlagAsUnhealthy(cluster, destination);
            }
        }
    }
}