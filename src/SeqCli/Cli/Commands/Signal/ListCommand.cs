﻿// Copyright 2018 Datalust Pty Ltd and Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.Signal
{
    [Command("signal", "list", "List available signals", Example="seqcli signal list")]
    class ListCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly EntityIdentityFeature _entityIdentity;
        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            _entityIdentity = Enable(new EntityIdentityFeature("signal", "list"));
            _output = Enable(new OutputFormatFeature(config.Output));
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var list = _entityIdentity.Id != null ?
                new[] { await connection.Signals.FindAsync(_entityIdentity.Id) } :
                (await connection.Signals.ListAsync())
                    .Where(signal => _entityIdentity.Title == null || _entityIdentity.Title == signal.Title)
                    .ToArray();

            foreach (var signal in list)
            {
                _output.WriteEntity(signal);
            }

            return 0;
        }
    }
}
