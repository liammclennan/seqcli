﻿// Copyright 2018 Datalust Pty Ltd
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
using Newtonsoft.Json;
using SeqCli.Forwarder.Cryptography;
using SeqCli.Util;

namespace SeqCli.Config;

public class ConnectionConfig
{
    const string ProtectedDataPrefix = "pd.";

    public string ServerUrl { get; set; } = "http://localhost:5341";

    [JsonProperty("apiKey")]
    public string? EncodedApiKey { get; set; }

    [JsonIgnore]
    public string? ApiKey
    {
        get
        {
            if (string.IsNullOrWhiteSpace(EncodedApiKey))
                return null;

            if (!OperatingSystem.IsWindows())
                return EncodedApiKey;

            if (!EncodedApiKey.StartsWith(ProtectedDataPrefix))
                return EncodedApiKey;

            return UserScopeDataProtection.Unprotect(EncodedApiKey.Substring(ProtectedDataPrefix.Length));
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                EncodedApiKey = null;
                return;
            }

            if (OperatingSystem.IsWindows())
                EncodedApiKey = $"{ProtectedDataPrefix}{UserScopeDataProtection.Protect(value)}";
            else
                EncodedApiKey = value;
        }
    }

    public string? GetApiKey(IStringDataProtector dataProtector)
    {
        throw new NotImplementedException();
    }
    
    public uint? PooledConnectionLifetimeMilliseconds { get; set; } = null;
    public ulong EventBodyLimitBytes { get; set; } = 256 * 1024;
    public ulong PayloadLimitBytes { get; set; } = 10 * 1024 * 1024;
}