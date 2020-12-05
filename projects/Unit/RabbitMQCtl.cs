﻿// This source code is dual-licensed under the Apache License, version
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

#pragma warning disable 2002

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.client.impl.Channel;

namespace RabbitMQ.Client.Unit
{
    public static class RabbitMQCtl
    {
        //
        // Shelling Out
        //
        public static Process ExecRabbitMQCtl(string args)
        {
            // Allow the path to the rabbitmqctl.bat to be set per machine
            string envVariable = Environment.GetEnvironmentVariable("RABBITMQ_RABBITMQCTL_PATH");
            string rabbitmqctlPath;

            if (envVariable != null)
            {
                var regex = new Regex(@"^DOCKER:(?<dockerMachine>.+)$");
                Match match = regex.Match(envVariable);

                if (match.Success)
                {
                    return ExecRabbitMQCtlUsingDocker(args, match.Groups["dockerMachine"].Value);
                }
                else
                {
                    rabbitmqctlPath = envVariable;
                }
            }
            else
            {
                // provided by the umbrella
                string umbrellaRabbitmqctlPath;
                // provided in PATH by a RabbitMQ installation
                string providedRabbitmqctlPath;

                if (IsRunningOnMonoOrDotNetCore())
                {
                    umbrellaRabbitmqctlPath = "../../../../../../rabbit/scripts/rabbitmqctl";
                    providedRabbitmqctlPath = "rabbitmqctl";
                }
                else
                {
                    umbrellaRabbitmqctlPath = @"..\..\..\..\..\..\rabbit\scripts\rabbitmqctl.bat";
                    providedRabbitmqctlPath = "rabbitmqctl.bat";
                }

                if (File.Exists(umbrellaRabbitmqctlPath))
                {
                    rabbitmqctlPath = umbrellaRabbitmqctlPath;
                }
                else
                {
                    rabbitmqctlPath = providedRabbitmqctlPath;
                }
            }

            return ExecCommand(rabbitmqctlPath, args);
        }

        public static Process ExecRabbitMQCtlUsingDocker(string args, string dockerMachineName)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            try
            {
                proc.StartInfo.FileName = "docker";
                proc.StartInfo.Arguments = $"exec {dockerMachineName} rabbitmqctl {args}";
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (stderr.Length > 0 || proc.ExitCode > 0)
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    ReportExecFailure("rabbitmqctl", args, $"{stderr}\n{stdout}");
                }

                return proc;
            }
            catch (Exception e)
            {
                ReportExecFailure("rabbitmqctl", args, e.Message);
                throw;
            }
        }

        public static Process ExecCommand(string command)
        {
            return ExecCommand(command, "");
        }

        public static Process ExecCommand(string command, string args)
        {
            return ExecCommand(command, args, null);
        }

        public static Process ExecCommand(string ctl, string args, string changeDirTo)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            if (changeDirTo != null)
            {
                proc.StartInfo.WorkingDirectory = changeDirTo;
            }

            string cmd;
            if (IsRunningOnMonoOrDotNetCore())
            {
                cmd = ctl;
            }
            else
            {
                cmd = "cmd.exe";
                args = $"/c \"\"{ctl}\" {args}\"";
            }

            try
            {
                proc.StartInfo.FileName = cmd;
                proc.StartInfo.Arguments = args;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;

                proc.Start();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (stderr.Length > 0 || proc.ExitCode > 0)
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    ReportExecFailure(cmd, args, $"{stderr}\n{stdout}");
                }

                return proc;
            }
            catch (Exception e)
            {
                ReportExecFailure(cmd, args, e.Message);
                throw;
            }
        }

        public static void ReportExecFailure(string cmd, string args, string msg)
        {
            Console.WriteLine($"Failure while running {cmd} {args}:\n{msg}");
        }

        public static bool IsRunningOnMonoOrDotNetCore()
        {
#if NETCOREAPP
            return true;
#else
            return Type.GetType("Mono.Runtime") != null;
#endif
        }

        //
        // Flow Control
        //

        public static Task BlockAsync(IConnection conn, Encoding encoding)
        {
            ExecRabbitMQCtl("set_vm_memory_high_watermark 0.000000001");
            // give rabbitmqctl some time to do its job
            Thread.Sleep(1200);
            return PublishAsync(conn, encoding);
        }

        public static async Task PublishAsync(IConnection conn, Encoding encoding)
        {
            await using IChannel ch = await conn.CreateChannelAsync().ConfigureAwait(false);
            await ch.PublishMessageAsync("amq.fanout", "", null, encoding.GetBytes("message")).ConfigureAwait(false);
        }


        public static void Unblock()
        {
            ExecRabbitMQCtl("set_vm_memory_high_watermark 0.4");
        }

        private static readonly Regex s_getConnectionName = new Regex(@"\{""connection_name"",""(?<connection_name>[^""]+)""\}");
        public class ConnectionInfo
        {
            public string Pid
            {
                get; set;
            }

            public string Name
            {
                get; set;
            }

            public ConnectionInfo(string pid, string name)
            {
                Pid = pid;
                Name = name;
            }

            public override string ToString()
            {
                return $"pid = {Pid}, name: {Name}";
            }
        }
        public static List<ConnectionInfo> ListConnections()
        {
            Process proc = ExecRabbitMQCtl("list_connections --silent pid client_properties");
            string stdout = proc.StandardOutput.ReadToEnd();

            try
            {
                // {Environment.NewLine} is not sufficient
                string[] splitOn = new string[] { "\r\n", "\n" };
                string[] lines = stdout.Split(splitOn, StringSplitOptions.RemoveEmptyEntries);
                // line: <rabbit@mercurio.1.11491.0>	{.../*client_properties*/...}
                return lines.Select(s =>
                {
                    string[] columns = s.Split('\t');
                    Debug.Assert(!string.IsNullOrEmpty(columns[0]), "columns[0] is null or empty!");
                    Debug.Assert(!string.IsNullOrEmpty(columns[1]), "columns[1] is null or empty!");
                    Match match = s_getConnectionName.Match(columns[1]);
                    Debug.Assert(match.Success, "columns[1] is not in expected format.");
                    return new ConnectionInfo(columns[0], match.Groups["connection_name"].Value);
                }).ToList();
            }
            catch (Exception)
            {
                Console.WriteLine($"Bad response from rabbitmqctl list_connections --silent pid client_properties{Environment.NewLine}{stdout}");
                throw;
            }
        }

        public static void CloseConnection(IConnection conn)
        {
            ConnectionInfo ci = ListConnections().First(x => conn.ClientProvidedName == x.Name);
            CloseConnection(ci.Pid);
        }

        public static void CloseAllConnections()
        {
            List<ConnectionInfo> cs = ListConnections();
            foreach (ConnectionInfo c in cs)
            {
                CloseConnection(c.Pid);
            }
        }

        public static void CloseConnection(string pid)
        {
            ExecRabbitMQCtl($"close_connection \"{pid}\" \"Closed via rabbitmqctl\"");
        }

        public static void RestartRabbitMQ()
        {
            StopRabbitMQ();
            Thread.Sleep(500);
            StartRabbitMQ();
        }

        public static void StopRabbitMQ()
        {
            ExecRabbitMQCtl("stop_app");
        }

        public static void StartRabbitMQ()
        {
            ExecRabbitMQCtl("start_app");
        }
    }
}
