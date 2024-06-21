using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;

namespace YarpMiddleware.Yarp
{
    /// <summary>
    /// Interface for custom load balancers.
    /// </summary>
    public interface ILoadBalancer
    {
        /// <summary>
        /// Picks the next destination using the implemented load balancing algorithm.
        /// </summary>
        /// <param name="destinations">The destinations to load balance over.</param>
        /// <param name="context">The http context of the request.</param>
        /// <param name="route">The configured route model.</param>
        /// <param name="config">The cluster configuration.</param>
        /// <returns>The selected destination.</returns>
        public DestinationState PickDestination(IReadOnlyList<DestinationState> destinations, 
            HttpContext context, 
            RouteModel route, 
            ClusterConfig config);
    }
}