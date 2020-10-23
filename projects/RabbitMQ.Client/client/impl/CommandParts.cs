using System;

namespace RabbitMQ.Client.Impl
{
    internal readonly struct CommandParts<T>
    {
        public readonly T Method;
        public readonly ContentHeaderBase Header;
        public readonly ReadOnlyMemory<byte> Body;

        public CommandParts(in T method, ContentHeaderBase header, ReadOnlyMemory<byte> body)
        {
            Method = method;
            Header = header;
            Body = body;
        }
    }
}
