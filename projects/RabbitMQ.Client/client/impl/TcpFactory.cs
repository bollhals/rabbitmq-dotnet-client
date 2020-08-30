using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.client.impl
{
    public sealed class TcpFactory : ITcpClientFactory
    {
        public async Task<ITcpClient> CreateAsync(AmqpTcpEndpoint endpoint, TimeSpan connectionTimeout, TimeSpan readTimeout, TimeSpan writeTimeout)
        {
            // Resolve the hostname to know if it's even possible to even try IPv6
            IPAddress[] adds = await Dns.GetHostAddressesAsync(endpoint.HostName).ConfigureAwait(false);
            TcpClient client;
            switch (endpoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    client = await TryConnectAsync(AddressFamily.InterNetwork, adds, endpoint.Port, connectionTimeout, true).ConfigureAwait(false);
                    break;
                case AddressFamily.InterNetworkV6:
                    client = await TryConnectAsync(AddressFamily.InterNetworkV6, adds, endpoint.Port, connectionTimeout, true).ConfigureAwait(false);
                    break;
                default:
                    client = await TryConnectAsync(AddressFamily.InterNetwork, adds, endpoint.Port, connectionTimeout, true).ConfigureAwait(false)
                             ?? await TryConnectAsync(AddressFamily.InterNetworkV6, adds, endpoint.Port, connectionTimeout, true).ConfigureAwait(false);
                    break;
            }

            if (client is null)
            {
                throw new ConnectFailureException("Connection failed", new ArgumentException($"No ip address could be resolved for {endpoint.HostName}"));
            }

            client.ReceiveTimeout = (int)readTimeout.TotalMilliseconds;
            client.SendTimeout = (int)writeTimeout.TotalMilliseconds;

            Stream stream = client.GetStream();
            if (endpoint.Ssl.Enabled)
            {
                try
                {
                    stream = SslHelper.TcpUpgrade(stream, endpoint.Ssl);
                }
                catch (Exception)
                {
                    client.Dispose();
                    throw;
                }
            }

            return new TcpClientAdapter(client, stream, endpoint);
        }

        private static async Task<TcpClient> TryConnectAsync(AddressFamily family, IPAddress[] addresses, int port, TimeSpan connectionTimeout, bool shallThrow)
        {
            IPAddress ip = FindAddressFor(addresses, family);

            if (!(ip is null))
            {
                TcpClient tcpClient = new TcpClient(family)
                {
                    NoDelay = true,
                    ReceiveBufferSize = 65536,
                    SendBufferSize = 65536,
                };
                try
                {
                    await tcpClient.ConnectAsync(ip, port).TimeoutAfter(connectionTimeout).ConfigureAwait(false);
                    return tcpClient;
                }
                catch (Exception e)
                {
                    tcpClient.Dispose();
                    if (shallThrow)
                    {
                        throw new ConnectFailureException("Connection failed", e);
                    }
                }
            }

            return null;
        }

        private static IPAddress FindAddressFor(IPAddress[] addresses, AddressFamily family)
        {
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == family)
                {
                    return address;
                }
            }

            if (addresses.Length == 1 && addresses[0].AddressFamily == AddressFamily.Unspecified)
            {
                return addresses[0];
            }

            return null;
        }
    }
}
