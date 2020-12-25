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
using System.Collections.Generic;
using RabbitMQ.Client.client.framing;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Impl
{
    internal abstract class SessionBase : ISession
    {
        private readonly object _shutdownLock = new object();
        private EventHandler<ShutdownEventArgs> _sessionShutdown;

        protected SessionBase(Connection connection, ushort channelNumber)
        {
            CloseReason = null;
            Connection = connection;
            ChannelNumber = channelNumber;
            if (channelNumber != 0)
            {
                connection.ConnectionShutdown += OnConnectionShutdown;
            }
        }

        public event EventHandler<ShutdownEventArgs> SessionShutdown
        {
            add
            {
                bool ok = false;
                if (CloseReason is null)
                {
                    lock (_shutdownLock)
                    {
                        if (CloseReason is null)
                        {
                            _sessionShutdown += value;
                            ok = true;
                        }
                    }
                }
                if (!ok)
                {
                    value(this, CloseReason);
                }
            }
            remove
            {
                lock (_shutdownLock)
                {
                    _sessionShutdown -= value;
                }
            }
        }

        public ushort ChannelNumber { get; }

        public ShutdownEventArgs CloseReason { get; set; }
        public CommandReceivedAction CommandReceived { get; set; }
        public Connection Connection { get; }

        public bool IsOpen
        {
            get { return CloseReason is null; }
        }

        public virtual void OnConnectionShutdown(object conn, ShutdownEventArgs reason)
        {
            Close(reason);
        }

        public virtual void OnSessionShutdown(ShutdownEventArgs reason)
        {
            Connection.ConnectionShutdown -= OnConnectionShutdown;
            EventHandler<ShutdownEventArgs> handler;
            lock (_shutdownLock)
            {
                handler = _sessionShutdown;
                _sessionShutdown = null;
            }

            handler?.Invoke(this, reason);
        }

        public override string ToString()
        {
            return $"{GetType().Name}#{ChannelNumber}:{Connection}";
        }

        public void Close(ShutdownEventArgs reason)
        {
            Close(reason, true);
        }

        public void Close(ShutdownEventArgs reason, bool notify)
        {
            if (CloseReason is null)
            {
                lock (_shutdownLock)
                {
                    if (CloseReason is null)
                    {
                        CloseReason = reason;
                    }
                }
            }
            if (notify)
            {
                OnSessionShutdown(CloseReason);
            }
        }

        public abstract bool HandleFrame(in InboundFrame frame);

        public void Notify()
        {
            // Ensure that we notify only when session is already closed
            // If not, throw exception, since this is a serious bug in the library
            if (CloseReason is null)
            {
                lock (_shutdownLock)
                {
                    if (CloseReason is null)
                    {
                        throw new Exception("Internal Error in Session.Close");
                    }
                }
            }
            OnSessionShutdown(CloseReason);
        }

        public virtual void Transmit<T>(in T cmd) where T : struct, IOutgoingAmqpMethod
        {
            if (CloseReason != null)
            {
                CheckCanSendWhileClosed(cmd.ProtocolCommandId);
            }

            Connection.Write(Framing.SerializeToFrames(cmd, ChannelNumber));
        }

        public void Transmit<T>(in T cmd, ContentHeaderBase header, ReadOnlyMemory<byte> body) where T : struct, IOutgoingAmqpMethod
        {
            if (CloseReason != null)
            {
                CheckCanSendWhileClosed(cmd.ProtocolCommandId);
            }

            Connection.Write(Framing.SerializeToFrames(cmd, header, body, ChannelNumber, Connection.MaxPayloadSize));
        }

        private void CheckCanSendWhileClosed(ProtocolCommandId commandId)
        {
            lock (_shutdownLock)
            {
                if (CloseReason != null)
                {
                    if (!Connection.Protocol.CanSendWhileClosed(commandId))
                    {
                        throw new AlreadyClosedException(CloseReason);
                    }
                }
            }
        }

        public void Transmit<T>(List<CommandParts<T>> cmds) where T : struct, IOutgoingAmqpMethod
        {
            int maxPayloadSize = Connection.MaxPayloadSize;
            ushort channelNumber = ChannelNumber;

            foreach (var parts in cmds)
            {
                Connection.Write(Framing.SerializeToFrames(parts.Method, parts.Header, parts.Body, channelNumber, maxPayloadSize));
            }
        }
    }
}
