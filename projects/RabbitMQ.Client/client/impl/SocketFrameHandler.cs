// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2020 VMware, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       https://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v2.0:
//
//---------------------------------------------------------------------------
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
//  Copyright (c) 2007-2020 VMware, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    internal sealed class SocketFrameHandler : IFrameHandler
    {
        private const int BufferSize = 65536;
        private readonly ITcpClient _tcpClient;
        private readonly Stream _outgoingStream;
        private readonly ChannelWriter<ReadOnlyMemory<byte>> _intermediateBufferWriter;
        private readonly ChannelReader<ReadOnlyMemory<byte>> _intermediateBufferReader;
        private readonly Task _writerTask;

        public SocketFrameHandler(ITcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            var channel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(
                new UnboundedChannelOptions
                {
                    AllowSynchronousContinuations = false,
                    SingleReader = true,
                    SingleWriter = false
                });

            _intermediateBufferReader = channel.Reader;
            _intermediateBufferWriter = channel.Writer;

            Stream stream = tcpClient.GetStream();
            IncomingStream = new BufferedStream(stream, BufferSize);
            _outgoingStream = new BufferedStream(stream, BufferSize);

            _writerTask = Task.Run(WriteLoop, CancellationToken.None);
        }

        public Stream IncomingStream { get; }

        public AmqpTcpEndpoint Endpoint => _tcpClient.Endpoint;
        public IPEndPoint LocalEndPoint => _tcpClient.LocalEndPoint;
        public IPEndPoint RemoteEndPoint => _tcpClient.RemoteEndPoint;
        public TimeSpan ReadTimeout { set => _tcpClient.ReadTimeout = value; }
        public TimeSpan WriteTimeout { set => _tcpClient.WriteTimeout = value; }

        public async Task CloseAsync()
        {
            if (_intermediateBufferWriter.TryComplete())
            {
                try
                {
                    await _writerTask.ConfigureAwait(false);
                }
                catch(Exception)
                {
                    // ignore, we are closing anyway
                }

                try
                {
                    _tcpClient.Dispose();
                }
                catch (Exception)
                {
                    // ignore, we are closing anyway
                }
            }
        }

        public void Write(ReadOnlyMemory<byte> memory)
        {
            _intermediateBufferWriter.TryWrite(memory);
        }

        private async Task WriteLoop()
        {
            while (await _intermediateBufferReader.WaitToReadAsync().ConfigureAwait(false))
            {
                _tcpClient.WaitUntilSenderIsReady();
                while (_intermediateBufferReader.TryRead(out ReadOnlyMemory<byte> memory))
                {
                    MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment);
                    await _outgoingStream.WriteAsync(segment.Array, segment.Offset, segment.Count).ConfigureAwait(false);
                    ArrayPool<byte>.Shared.Return(segment.Array);
                }

                await _outgoingStream.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}
