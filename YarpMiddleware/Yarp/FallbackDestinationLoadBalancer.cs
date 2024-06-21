using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace YarpMiddleware.Yarp
{
    /// <summary>
    /// Load balancer used in conjunction with the <see cref="FallbackDestinationMiddleware"/> which implements either
    /// the configured or default <see cref="LoadBalancingPolicies.PowerOfTwoChoices"/> load balancing policy.
    /// </summary>
    public class FallbackDestinationLoadBalancer: ILoadBalancer
    {
        private readonly Dictionary<string, ILoadBalancingPolicy> loadBalancingPolicies;
    
        public FallbackDestinationLoadBalancer(IEnumerable<ILoadBalancingPolicy> loadBalancingPolicies)
        {
            this.loadBalancingPolicies = loadBalancingPolicies.ToDictionary(k => k.Name, v => v);
        }
    
        public DestinationState PickDestination(
            IReadOnlyList<DestinationState> destinations,
            HttpContext context, 
            RouteModel route,
            ClusterConfig config)
        {
            switch (destinations.Count)
            {
                case 0:
                    return null;
            
                case 1:
                    return destinations[0];
            
                default:
                {
                    var loadBalancingPolicy = config.LoadBalancingPolicy ?? LoadBalancingPolicies.PowerOfTwoChoices;
                    var currentPolicy = loadBalancingPolicies[loadBalancingPolicy];
                    return currentPolicy.PickDestination(context, route.Cluster!, destinations);
                }
            }
        }
    }
}