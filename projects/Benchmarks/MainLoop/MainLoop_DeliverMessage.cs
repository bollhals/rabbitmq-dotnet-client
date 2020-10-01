using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using RabbitMQ.Client;
using RabbitMQ.Client.client.framing;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Unit.FakeInternals;
using BasicDeliver = RabbitMQ.Client.Framing.Impl.BasicDeliver;

namespace Benchmarks.MainLoop
{
    [ShortRunJob]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    public class MainLoop_DeliverMessage
    {
        [Params(1, 10, 100, 1000)]
        public static int Count;

        private readonly IConnection _connection;
        private readonly IModel _model;
        private readonly FakeStream _stream;
        private readonly CountdownEvent _signal;

        public MainLoop_DeliverMessage()
        {
            var factory = new ConnectionFactory();
            _stream = new FakeStream();
            var client = new FakeTcpClient(_stream);
            factory.TcpClientFactory = new FakeTcpClientFactory(client);

            _stream.AddCreateConnection();
            _connection = factory.CreateConnection();
            _stream.AddCreateModel(1);
            _model = _connection.CreateModel();

            _signal = new CountdownEvent(0);
            var consumer = new EventingBasicConsumer(_model);
            consumer.Received += (sender, args) => _signal.Signal();
            _model.BasicConsume(string.Empty, false, string.Empty, false, false, new Dictionary<string, object>(), consumer);
            _stream.AddReply(ProtocolCommandId.BasicDeliver, 1, new BasicDeliver(string.Empty, 0, false, string.Empty, string.Empty), new RabbitMQ.Client.Framing.BasicProperties(), Encoding.UTF8.GetBytes("Hi"));
        }

        [Benchmark]
        public void Deliver()
        {
            var count = Count;
            _signal.Reset(count);
            for (int i = 0; i < count; i++)
            {
                _stream.TriggerRead(ProtocolCommandId.BasicDeliver);
            }
            _signal.Wait();
        }
    }
}
