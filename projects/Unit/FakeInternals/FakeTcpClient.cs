using System;
using System.IO;
using System.Net;

namespace RabbitMQ.Client.Unit.FakeInternals
{
    public sealed class FakeTcpClient : ITcpClient
    {
        private readonly Stream _readStream;

        public AmqpTcpEndpoint Endpoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public TimeSpan ReadTimeout { get; set; }
        public TimeSpan WriteTimeout { get; set; }

        public FakeTcpClient(Stream readStream)
        {
            _readStream = readStream;
        }

        public Stream GetStream() => _readStream;

        public void WaitUntilSenderIsReady() { }
        public void Dispose() { _readStream.Dispose(); }
    }
}
