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
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Impl
{
    class SimpleBlockingRpcContinuation : IRpcContinuation
    {
        public readonly BlockingCell<Either<MethodBase, ShutdownEventArgs>> m_cell = new BlockingCell<Either<MethodBase, ShutdownEventArgs>>();

        public virtual MethodBase GetReply()
        {
            Either<MethodBase, ShutdownEventArgs> result = m_cell.WaitForValue();
            switch (result.Alternative)
            {
                case EitherAlternative.Left:
                    return result.LeftValue;
                case EitherAlternative.Right:
                    throw new OperationInterruptedException(result.RightValue);
                default:
                    return default;
            }
        }

        public virtual MethodBase GetReply(TimeSpan timeout)
        {
            Either<MethodBase, ShutdownEventArgs> result = m_cell.WaitForValue(timeout);
            switch (result.Alternative)
            {
                case EitherAlternative.Left:
                    return result.LeftValue;
                case EitherAlternative.Right:
                    throw new OperationInterruptedException(result.RightValue);
                default:
                    return default;
            }
        }

        public virtual void HandleCommand(MethodBase methodBase)
        {
            m_cell.ContinueWithValue(Either<MethodBase, ShutdownEventArgs>.Left(methodBase));
        }

        public virtual void HandleModelShutdown(ShutdownEventArgs reason)
        {
            m_cell.ContinueWithValue(Either<MethodBase, ShutdownEventArgs>.Right(reason));
        }
    }
}
