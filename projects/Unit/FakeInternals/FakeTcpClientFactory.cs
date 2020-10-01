using System;
using System.Threading.Tasks;
using RabbitMQ.Client.client.impl;

namespace RabbitMQ.Client.Unit.FakeInternals
{
    public sealed class FakeTcpClientFactory : ITcpClientFactory
    {
        private readonly FakeTcpClient _client;

        public FakeTcpClientFactory(FakeTcpClient client)
        {
            _client = client;
        }

        public Task<ITcpClient> CreateAsync(AmqpTcpEndpoint endpoint, TimeSpan connectionTimeout, TimeSpan readTimeout, TimeSpan writeTimeout)
        {
            _client.Endpoint = endpoint;
            return Task.FromResult<ITcpClient>(_client);
        }
    }
}
