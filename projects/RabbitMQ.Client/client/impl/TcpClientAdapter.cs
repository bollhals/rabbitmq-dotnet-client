using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace RabbitMQ.Client.Impl
{
    /// <summary>
    /// Simple wrapper around TcpClient.
    /// </summary>
    internal sealed class TcpClientAdapter : ITcpClient
    {
        private const int MsInUs = 1000;
        private readonly TcpClient _client;
        private readonly Stream _stream;
        private int _pollTimeout;

        public TcpClientAdapter(TcpClient client, Stream stream, AmqpTcpEndpoint endpoint)
        {
            _client = client;
            _stream = stream;
            Endpoint = endpoint;
            _pollTimeout = _client.SendTimeout * MsInUs;
        }

        public AmqpTcpEndpoint Endpoint { get; }
        public IPEndPoint LocalEndPoint => (IPEndPoint)_client.Client.LocalEndPoint;
        public IPEndPoint RemoteEndPoint => (IPEndPoint)_client.Client.RemoteEndPoint;

        public TimeSpan ReadTimeout
        {
            get => TimeSpan.FromMilliseconds(_client.ReceiveTimeout);
            set => _client.ReceiveTimeout = (int)value.TotalMilliseconds;
        }

        public TimeSpan WriteTimeout
        {
            get => TimeSpan.FromMilliseconds(_client.SendTimeout);
            set
            {
                int timeout = (int)value.TotalMilliseconds;
                _client.SendTimeout = timeout;
                _pollTimeout = timeout * MsInUs;
            }
        }

        public Stream GetStream()
        {
            return _stream;
        }

        public void WaitUntilSenderIsReady()
        {
            _client.Client.Poll(_pollTimeout, SelectMode.SelectWrite);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
