using System;
using System.IO;
using System.Net;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Wrapper interface for a specific endpoint client.
    /// </summary>
    public interface ITcpClient : IDisposable
    {
        AmqpTcpEndpoint Endpoint { get; }
        IPEndPoint LocalEndPoint { get; }
        IPEndPoint RemoteEndPoint { get; }

        TimeSpan ReadTimeout { get; set; }
        TimeSpan WriteTimeout { get; set; }

        Stream GetStream();
        void WaitUntilSenderIsReady();
    }
}
