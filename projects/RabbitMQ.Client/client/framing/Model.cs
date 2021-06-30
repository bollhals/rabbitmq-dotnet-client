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
using RabbitMQ.Client.ConsumerDispatching;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Framing.Impl
{
    internal class Model : ModelBase
    {
        public Model(bool dispatchAsync, int concurrency, ISession session) : base(dispatchAsync, concurrency, session)
        {
        }

        public override void ConnectionTuneOk(ushort channelMax, uint frameMax, ushort heartbeat)
        {
            ModelSend(new ConnectionTuneOk(channelMax, frameMax, heartbeat));
        }

        public override void _Private_BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            ModelSend(new BasicPublish(exchange, routingKey, mandatory, default), (BasicProperties) basicProperties, body);
        }

        public override void _Private_BasicPublishMemory(ReadOnlyMemory<byte> exchange, ReadOnlyMemory<byte> routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            ModelSend(new BasicPublishMemory(exchange, routingKey, mandatory, default), (BasicProperties) basicProperties, body);
        }

        public override void _Private_ChannelClose(ushort replyCode, string replyText, ushort classId, ushort methodId)
        {
            ModelSend(new ChannelClose(replyCode, replyText, classId, methodId));
        }

        public override void _Private_ChannelCloseOk()
        {
            ModelSend(new ChannelCloseOk());
        }

        public override void _Private_ChannelFlowOk(bool active)
        {
            ModelSend(new ChannelFlowOk(active));
        }

        public override void _Private_ChannelOpen()
        {
            ModelRpc(new ChannelOpen(), ProtocolCommandId.ChannelOpenOk, ContinuationTimeout);
        }

        public override void _Private_ConfirmSelect(bool nowait)
        {
            var method = new ConfirmSelect(nowait);
            if (nowait)
            {
                ModelSend(method);
            }
            else
            {
                ModelRpc(method, ProtocolCommandId.ConfirmSelectOk, ContinuationTimeout);
            }
        }

        public override void _Private_ConnectionCloseOk()
        {
            ModelSend(new ConnectionCloseOk());
        }

        public override void _Private_UpdateSecret(byte[] newSecret, string reason)
        {
            ModelRpc(new ConnectionUpdateSecret(newSecret, reason), ProtocolCommandId.ConnectionUpdateSecretOk, ContinuationTimeout);
        }

        public override void _Private_ExchangeBind(string destination, string source, string routingKey, bool nowait, IDictionary<string, object> arguments)
        {
            ExchangeBind method = new ExchangeBind(destination, source, routingKey, nowait, arguments);
            if (nowait)
            {
                ModelSend(method);
            }
            else
            {
                ModelRpc(method, ProtocolCommandId.ExchangeBindOk, ContinuationTimeout);
            }
        }

        public override void _Private_ExchangeDeclare(string exchange, string type, bool passive, bool durable, bool autoDelete, bool @internal, bool nowait, IDictionary<string, object> arguments)
        {
            ExchangeDeclare method = new ExchangeDeclare(exchange, type, passive, durable, autoDelete, @internal, nowait, arguments);
            if (nowait)
            {
                ModelSend(method);
            }
            else
            {
                ModelRpc(method, ProtocolCommandId.ExchangeDeclareOk, ContinuationTimeout);
            }
        }

        public override void _Private_ExchangeDelete(string exchange, bool ifUnused, bool nowait)
        {
            ExchangeDelete method = new ExchangeDelete(exchange, ifUnused, nowait);
            if (nowait)
            {
                ModelSend(method);
            }
            else
            {
                ModelRpc(method, ProtocolCommandId.ExchangeDeleteOk, ContinuationTimeout);
            }
        }

        public override void _Private_ExchangeUnbind(string destination, string source, string routingKey, bool nowait, IDictionary<string, object> arguments)
        {
            ExchangeUnbind method = new ExchangeUnbind(destination, source, routingKey, nowait, arguments);
            if (nowait)
            {
                ModelSend(method);
            }
            else
            {
                ModelRpc(method, ProtocolCommandId.ExchangeUnbindOk, ContinuationTimeout);
            }
        }

        public override void _Private_QueueBind(string queue, string exchange, string routingKey, bool nowait, IDictionary<string, object> arguments)
        {
            QueueBind method = new QueueBind(queue, exchange, routingKey, nowait, arguments);
            if (nowait)
            {
                ModelSend(method);
            }
            else
            {
                ModelRpc(method, ProtocolCommandId.QueueBindOk, ContinuationTimeout);
            }
        }

        public override uint _Private_QueueDelete(string queue, bool ifUnused, bool ifEmpty, bool nowait)
        {
            QueueDelete method = new QueueDelete(queue, ifUnused, ifEmpty, nowait);
            if (nowait)
            {
                ModelSend(method);
                return 0xFFFFFFFF;
            }

            return ModelRpc(method, ProtocolCommandId.QueueDeleteOk, memory => new QueueDeleteOk(memory.Span)._messageCount, ContinuationTimeout);
        }

        public override uint _Private_QueuePurge(string queue, bool nowait)
        {
            QueuePurge method = new QueuePurge(queue, nowait);
            if (nowait)
            {
                ModelSend(method);
                return 0xFFFFFFFF;
            }

            return ModelRpc(method, ProtocolCommandId.QueuePurgeOk, memory => new QueuePurgeOk(memory.Span)._messageCount, ContinuationTimeout);
        }

        public override void BasicAck(ulong deliveryTag, bool multiple)
        {
            ModelSend(new BasicAck(deliveryTag, multiple));
        }

        public override void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            ModelSend(new BasicNack(deliveryTag, multiple, requeue));
        }

        public sealed override void BasicCancel(string consumerTag)
        {
            consumerTag = ModelRpc(
                new BasicCancel(consumerTag, false),
                ProtocolCommandId.BasicCancelOk,
                memory => new BasicCancelOk(memory.Span)._consumerTag,
                ContinuationTimeout);
            ConsumerDispatcher.HandleBasicCancelOk(consumerTag);
        }

        public sealed override void BasicCancelNoWait(string consumerTag)
        {
            ModelSend(new BasicCancel(consumerTag, true));
            ConsumerDispatcher.GetAndRemoveConsumer(consumerTag);
        }

        public sealed override string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
        {
            // TODO: Replace with flag
            if (ConsumerDispatcher is AsyncConsumerDispatcher)
            {
                if (!(consumer is IAsyncBasicConsumer))
                {
                    // TODO: Friendly message
                    throw new InvalidOperationException("In the async mode you have to use an async consumer");
                }
            }

            consumerTag = (string)ModelRpc(
                new BasicConsume(queue, consumerTag, noLocal, autoAck, exclusive, false, arguments),
                ProtocolCommandId.BasicConsumeOk,
                consumer);

            ConsumerDispatcher.HandleBasicConsumeOk(consumer, consumerTag);
            return consumerTag;
        }

        public sealed override BasicGetResult BasicGet(string queue, bool autoAck)
        {
            return ModelRpc(
                new BasicGet(queue, autoAck),
                (in IncomingCommand cmd) =>
                {
                    if (cmd.CommandId == ProtocolCommandId.BasicGetOk)
                    {
                        var method = new BasicGetOk(cmd.MethodBytes.Span);
                        cmd.ReturnMethodBuffer();
                        return new BasicGetResult(
                            AdjustDeliveryTag(method._deliveryTag),
                            method._redelivered,
                            method._exchange,
                            method._routingKey,
                            method._messageCount,
                            (IBasicProperties)cmd.Header,
                            cmd.Body,
                            cmd.TakeoverPayload());
                    }

                    cmd.ReturnMethodBuffer();
                    if (cmd.CommandId == ProtocolCommandId.BasicGetEmpty)
                    {
                        return null;
                    }

                    throw new UnexpectedMethodException(cmd.CommandId, ProtocolCommandId.BasicGetOk);
                });
        }

        public override void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            ModelRpc(new BasicQos(prefetchSize, prefetchCount, global), ProtocolCommandId.BasicQosOk, ContinuationTimeout);
        }

        public sealed override void BasicRecover(bool requeue)
        {
            ModelRpc(new BasicRecover(requeue), ProtocolCommandId.BasicRecoverOk, ContinuationTimeout);
            RaiseRecoverOk();
        }

        public override void BasicRecoverAsync(bool requeue)
        {
            ModelSend(new BasicRecoverAsync(requeue));
        }

        public override void BasicReject(ulong deliveryTag, bool requeue)
        {
            ModelSend(new BasicReject(deliveryTag, requeue));
        }

        public override IBasicProperties CreateBasicProperties()
        {
            return new BasicProperties();
        }

        public override void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            ModelRpc(new QueueUnbind(queue, exchange, routingKey, arguments), ProtocolCommandId.QueueUnbindOk, ContinuationTimeout);
        }

        public override void TxCommit()
        {
            ModelRpc(new TxCommit(), ProtocolCommandId.TxCommitOk, ContinuationTimeout);
        }

        public override void TxRollback()
        {
            ModelRpc(new TxRollback(), ProtocolCommandId.TxRollbackOk, ContinuationTimeout);
        }

        public override void TxSelect()
        {
            ModelRpc(new TxSelect(), ProtocolCommandId.TxSelectOk, ContinuationTimeout);
        }

        protected sealed override Client.QueueDeclareOk QueueDeclare(string queue, bool passive, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            return ModelRpc(new QueueDeclare(queue, passive, durable, exclusive, autoDelete, false, arguments),
                ProtocolCommandId.QueueDeclareOk,
                memory =>
                {
                    var method = new QueueDeclareOk(memory.Span);
                    return new Client.QueueDeclareOk(method._queue, method._messageCount, method._consumerCount);
                },
                ContinuationTimeout);
        }

        protected sealed override void QueueDeclareNoWait(string queue, bool passive, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            ModelSend(new QueueDeclare(queue, passive, durable, exclusive, autoDelete, true, arguments));
        }

        internal sealed override void ConnectionOpen(string virtualHost)
        {
            ModelRpc(new ConnectionOpen(virtualHost), ProtocolCommandId.ConnectionOpenOk, HandshakeContinuationTimeout);
        }

        internal sealed override ConnectionSecureOrTune ConnectionSecureOk(byte[] response)
        {
           return ModelRpc(
                new ConnectionSecureOk(response),
                ProtocolCommandId.ConnectionSecure,
                memory =>
                {
                    return new ConnectionSecureOrTune
                    {
                        m_challenge = new ConnectionSecure(memory.Span)._challenge
                    };
                },
                HandshakeContinuationTimeout);
        }

        internal sealed override ConnectionSecureOrTune ConnectionStartOk(IDictionary<string, object> clientProperties, string mechanism, byte[] response, string locale)
        {
            return ModelRpc(
                new ConnectionStartOk(clientProperties, mechanism, response, locale),
                ProtocolCommandId.ConnectionTune,
                memory =>
                {
                    var connectionTune = new ConnectionTune(memory.Span);
                    return new ConnectionSecureOrTune
                    {
                        m_tuneDetails = new ConnectionTuneDetails
                        {
                            m_channelMax = connectionTune._channelMax,
                            m_frameMax = connectionTune._frameMax,
                            m_heartbeatInSeconds = connectionTune._heartbeat
                        }
                    };
                },
                HandshakeContinuationTimeout);
        }

        protected override bool DispatchAsynchronous(in IncomingCommand cmd)
        {
            switch (cmd.CommandId)
            {
                case ProtocolCommandId.BasicDeliver:
                {
                    HandleBasicDeliver(in cmd);
                    return true;
                }
                case ProtocolCommandId.BasicAck:
                {
                    HandleBasicAck(in cmd);
                    return true;
                }
                case ProtocolCommandId.BasicCancel:
                {
                    HandleBasicCancel(in cmd);
                    return true;
                }
                case ProtocolCommandId.BasicConsumeOk:
                {
                    HandleBasicConsumeOk(in cmd);
                    return true;
                }
                case ProtocolCommandId.BasicNack:
                {
                    HandleBasicNack(in cmd);
                    return true;
                }
                case ProtocolCommandId.BasicReturn:
                {
                    HandleBasicReturn(in cmd);
                    return true;
                }
                case ProtocolCommandId.ChannelClose:
                {
                    HandleChannelClose(in cmd);
                    return true;
                }
                case ProtocolCommandId.ChannelCloseOk:
                {
                    cmd.ReturnMethodBuffer();
                    HandleChannelCloseOk();
                    return true;
                }
                case ProtocolCommandId.ChannelFlow:
                {
                    HandleChannelFlow(in cmd);
                    return true;
                }
                case ProtocolCommandId.ConnectionBlocked:
                {
                    HandleConnectionBlocked(in cmd);
                    return true;
                }
                case ProtocolCommandId.ConnectionClose:
                {
                    HandleConnectionClose(in cmd);
                    return true;
                }
                case ProtocolCommandId.ConnectionStart:
                {
                    HandleConnectionStart(in cmd);
                    return true;
                }
                case ProtocolCommandId.ConnectionUnblocked:
                {
                    cmd.ReturnMethodBuffer();
                    HandleConnectionUnblocked();
                    return true;
                }
                default: return false;
            }
        }
    }
}
