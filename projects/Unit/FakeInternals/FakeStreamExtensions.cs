using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client.client.framing;
using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Unit.FakeInternals
{
    internal static class FakeStreamExtensions
    {
        public static void AddCreateConnection(this FakeStream stream)
        {
            stream.AddReply((ProtocolCommandId)uint.MaxValue, 0, new ConnectionStart(0, 9, new Dictionary<string, object>(), Encoding.UTF8.GetBytes("PLAIN"), Array.Empty<byte>()));
            stream.AddReply(ProtocolCommandId.ConnectionStartOk, 0, new ConnectionTune(0, 0, 0));
            stream.AddNoReply(ProtocolCommandId.ConnectionTuneOk);
            stream.AddReply(ProtocolCommandId.ConnectionOpen, 0, new ConnectionOpenOk(string.Empty));
            stream.AddReply(ProtocolCommandId.ConnectionClose, 0, new ConnectionCloseOk());
        }

        public static void AddCreateModel(this FakeStream stream, int sessionId)
        {
            stream.AddReply(ProtocolCommandId.ChannelOpen, sessionId, new ChannelOpenOk(Array.Empty<byte>()));
            stream.AddReply(ProtocolCommandId.BasicConsume, sessionId, new BasicConsumeOk(string.Empty));
        }
    }
}
