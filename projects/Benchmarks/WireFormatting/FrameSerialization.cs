using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;

using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Impl;
using BasicProperties = RabbitMQ.Client.Framing.BasicProperties;

namespace RabbitMQ.Benchmarks
{
    [Config(typeof(Config))]
    [BenchmarkCategory("Frames")]
    public class FramesSerializationBase
    {
        [GlobalSetup]
        public virtual void SetUp() { }

        public virtual void Overhead()
        {
            ReadOnlyMemory<byte> frame = ArrayPool<byte>.Shared.Rent(200);
            MemoryMarshal.TryGetArray(frame, out var seg);
            ArrayPool<byte>.Shared.Return(seg.Array);
        }
    }

    public class FramesBasicPublish : FramesSerializationBase
    {
        private readonly BasicPublish _basicPublish = new BasicPublish("Exchange.Name", "Routing Key Example", false, false);
        private readonly BasicPublishMemory _basicPublishMemory = new BasicPublishMemory(
            Encoding.UTF8.GetBytes("Exchange.Name"), Encoding.UTF8.GetBytes("Routing Key Example"), false, false);
        private readonly BasicProperties _properties = new BasicProperties { AppId = "Application id", MessageId = "Random message id" };
        private readonly ReadOnlyMemory<byte> _body = new byte[512];

        [Benchmark]
        public override void Overhead()
        {
            base.Overhead();
        }

        [Benchmark(Baseline = true)]
        public void BasicPublish()
        {
            var frame = Framing.SerializeToFrames(_basicPublish, _properties, _body, 1, 100000);
            MemoryMarshal.TryGetArray(frame, out var seg);
            ArrayPool<byte>.Shared.Return(seg.Array);
        }

        [Benchmark]
        public void BasicPublishMemory()
        {
            var frame = Framing.SerializeToFrames(_basicPublishMemory, _properties, _body, 1, 100000);
            MemoryMarshal.TryGetArray(frame, out var seg);
            ArrayPool<byte>.Shared.Return(seg.Array);
        }
    }

    public class FramesBasicAck : FramesSerializationBase
    {
        private readonly BasicAck _basicAck = new BasicAck(400, false);

        [Benchmark]
        public override void Overhead()
        {
            base.Overhead();
        }

        [Benchmark(Baseline = true)]
        public void BasicAck()
        {
            var frame = Framing.SerializeToFrames(_basicAck, 1);
            MemoryMarshal.TryGetArray(frame, out var seg);
            ArrayPool<byte>.Shared.Return(seg.Array);
        }
    }
}
