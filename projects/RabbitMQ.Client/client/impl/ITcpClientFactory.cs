using System;
using System.Threading.Tasks;

namespace RabbitMQ.Client.client.impl
{
    /// <summary>
    /// A factory to create <see cref="ITcpClient"/>.
    /// </summary>
    public interface ITcpClientFactory
    {
        /// <summary>
        /// Creates a new connected client.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="connectionTimeout">The timeout for the connection attempt.</param>
        /// <param name="readTimeout">The read timeout.</param>
        /// <param name="writeTimeout">The write timeout.</param>
        /// <returns></returns>
        Task<ITcpClient> CreateAsync(AmqpTcpEndpoint endpoint, TimeSpan connectionTimeout, TimeSpan readTimeout, TimeSpan writeTimeout);
    }
}
