using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using RabbitMQ.Client.client.framing;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Unit.FakeInternals
{
    internal sealed class FakeStream : Stream
    {
        private readonly Dictionary<ProtocolCommandId, byte[]> _replies;
        private readonly BlockingCollection<ProtocolCommandId> _queue;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public FakeStream()
        {
            _replies = new Dictionary<ProtocolCommandId, byte[]>();
            _queue = new BlockingCollection<ProtocolCommandId>();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            byte[] array;
            do
            {
                var commandId = _queue.Take();
                array = _replies[commandId];
            } while (array is null);
            array.CopyTo(buffer.AsSpan(offset, count));
            return array.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public void AddReply<T>(ProtocolCommandId commandId, int session, T method, Framing.BasicProperties properties, byte[] body)
            where T : MethodBase
        {
            int methodPayloadSize = method.GetRequiredBufferSize();
            int propertiesPayloadSize = properties.GetRequiredPayloadBufferSize();
            var buffer = new byte[1 + 2 + 4 + 4 + methodPayloadSize + 1 + // Method frame
                                  1 + 2 + 4 + 2 + 2 + 8 + propertiesPayloadSize + 1 + // Header frame
                                  1 + 2 + 4 + body.Length + 1]; // Body frame
            int offset = Impl.Framing.Method.WriteTo(buffer, (ushort)session, method);
            offset += Impl.Framing.Header.WriteTo(buffer.AsSpan(offset), (ushort)session, properties, body.Length);
            Impl.Framing.BodySegment.WriteTo(buffer.AsSpan(offset), (ushort)session, body);

            _replies[commandId] = buffer;
        }

        public void AddReply<T>(ProtocolCommandId commandId, int session, T method)
            where T : MethodBase
        {
            int payloadSize = method.GetRequiredBufferSize();
            var buffer = new byte[1 + 2 + 4 + 4 + payloadSize + 1];
            Impl.Framing.Method.WriteTo(buffer, (ushort)session, method);
            _replies[commandId] = buffer;
        }

        public void AddNoReply(ProtocolCommandId commandId)
        {
            _replies[commandId] = null;
        }

        public void TriggerRead(ProtocolCommandId commandId)
        {
            _queue.Add(commandId);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer[0] == Constants.FrameHeartbeat)
            {
                return;
            }

            if (count == 8)
            {
                _queue.Add((ProtocolCommandId)uint.MaxValue);
            }
            else
            {
                _queue.Add((ProtocolCommandId)NetworkOrderDeserializer.ReadUInt32(buffer.AsSpan(7)));
            }
        }
    }
}
