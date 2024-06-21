using System;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Model;

namespace YarpMiddleware.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IDestinationHealthUpdater"/>.
    /// </summary>
    public static class DestinationHealthUpdaterExtensions
    {
        private static readonly TimeSpan DefaultReactivationPeriod = TimeSpan.FromSeconds(60);
    
        /// <summary>
        /// Updates the state of the destination to <see cref="DestinationHealth.Unhealthy"/> removing it from the list
        /// of available destinations for a configured time interval.
        /// </summary>
        public static void FlagAsUnhealthy(this IDestinationHealthUpdater healthUpdater,
            ClusterState cluster, 
            DestinationState destination)
        {        
            var reactivationPeriod = cluster.Model
                .Config
                .HealthCheck?
                .Passive?
                .ReactivationPeriod ?? DefaultReactivationPeriod;
        
            healthUpdater.SetPassive(cluster, destination, DestinationHealth.Unhealthy, reactivationPeriod);
        }
    }
}