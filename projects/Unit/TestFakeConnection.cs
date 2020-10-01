using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using RabbitMQ.Client.client.framing;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Unit.FakeInternals;
using BasicDeliver = RabbitMQ.Client.Framing.Impl.BasicDeliver;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestFakeConnection
    {
        [Test]
        public void FakeDeliverOneMessage()
        {
            var factory = new ConnectionFactory();
            var stream = new FakeStream();
            var client = new FakeTcpClient(stream);
            factory.TcpClientFactory = new FakeTcpClientFactory(client);

            stream.AddCreateConnection();
            stream.AddCreateModel(1);

            using (var connection = factory.CreateConnection())
            {
                IModel model = connection.CreateModel();

                using (var resetEvent = new AutoResetEvent(false))
                {
                    var consumer = new EventingBasicConsumer(model);
                    consumer.Received += (sender, args) =>
                    {
                        resetEvent.Set();
                    };
                    model.BasicConsume(string.Empty, false, string.Empty, false, false, new Dictionary<string, object>(), consumer);

                    stream.AddReply(ProtocolCommandId.BasicDeliver, 1, new BasicDeliver(string.Empty, 0, false, string.Empty, string.Empty), new Framing.BasicProperties(), Encoding.UTF8.GetBytes("Hi"));
                    stream.TriggerRead(ProtocolCommandId.BasicDeliver);
                    bool signaled = resetEvent.WaitOne(TimingFixture.TestTimeout);

                    Assert.IsTrue(signaled);
                }
            }
        }
    }
}
