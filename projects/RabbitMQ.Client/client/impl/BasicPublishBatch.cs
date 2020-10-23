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
using System.Runtime.CompilerServices;
using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Impl
{
    internal sealed class BasicPublishBatch : IBasicPublishBatch
    {
        private readonly ModelBase _model;
        private readonly int _sizeHint;
        private List<CommandParts<BasicPublish>> _publishCommands;
        private List<CommandParts<BasicPublishMemory>> _publishMemoryCommands;

        internal BasicPublishBatch (ModelBase model)
        {
            _model = model;
            _sizeHint = 4;
        }

        internal BasicPublishBatch (ModelBase model, int sizeHint)
        {
            _model = model;
            _sizeHint = sizeHint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UsingBasicPublish()
        {
            if (_publishCommands is null)
            {
                if (_publishMemoryCommands is null)
                {
                    _publishCommands = new List<CommandParts<BasicPublish>>(_sizeHint);
                }
                else
                {
                    ThrowDoNotMixPublishMethods();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UsingBasicPublishMemory()
        {
            if (_publishMemoryCommands is null)
            {
                if (_publishCommands is null)
                {
                    _publishMemoryCommands = new List<CommandParts<BasicPublishMemory>>(_sizeHint);
                }
                else
                {
                    ThrowDoNotMixPublishMethods();
                }
            }
        }

        private static void ThrowDoNotMixPublishMethods()
        {
            throw new InvalidOperationException($"Not allowed to mix the {nameof(Add)} method for string and ReadOnlyMemory<byte>.");
        }

        public void Add(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            UsingBasicPublish();
            var method = new BasicPublish(exchange, routingKey, mandatory, default);
            _publishCommands.Add(new CommandParts<BasicPublish>(method, (ContentHeaderBase)(basicProperties ?? _model._emptyBasicProperties), body));
        }

        public void Add(CachedString exchange, CachedString routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            UsingBasicPublishMemory();
            var method = new BasicPublishMemory(exchange.Bytes, routingKey.Bytes, mandatory, default);
            _publishMemoryCommands.Add(new CommandParts<BasicPublishMemory>(method, (ContentHeaderBase)(basicProperties ?? _model._emptyBasicProperties), body));
        }

        public void Publish()
        {
            if (_publishCommands != null)
            {
                _model.SendCommands(_publishCommands);
                return;
            }

            if (_publishMemoryCommands != null)
            {
                _model.SendCommands(_publishMemoryCommands);
            }
        }
    }
}
