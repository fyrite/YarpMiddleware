using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;

namespace YarpMiddleware.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Determines whether the proxied request was successful or not.
        /// </summary>
        /// <remarks>
        /// Logic for this method was taken from the Yarp implementation of
        /// <see cref="Yarp.ReverseProxy.Health.TransportFailureRateHealthPolicy"/>.
        /// </remarks>
        public static bool DetermineIfDestinationFailed(this HttpContext context)
        {
            var errorFeature = context.GetForwarderErrorFeature();
            if (errorFeature is null)
            {
                return false;
            }

            if (context.RequestAborted.IsCancellationRequested)
            {
                // The client disconnected/canceled the request - the failure may not be the destination's fault
                return false;
            }

            return errorFeature.Error is ForwarderError.Request 
                or ForwarderError.RequestTimedOut 
                or ForwarderError.RequestBodyDestination 
                or ForwarderError.ResponseBodyDestination
                or ForwarderError.UpgradeRequestDestination
                or ForwarderError.UpgradeResponseDestination;
        }
    }
}