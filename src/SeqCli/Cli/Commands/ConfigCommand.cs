// Copyright 2018-2021 Datalust Pty Ltd
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands;

[Command("config", "View and set fields in the `SeqCli.json` file; run with no arguments to list all fields")]
class ConfigCommand : Command
{
    string? _key, _value;
    bool _clear;

    public ConfigCommand()
    {
        Options.Add("k|key=", "The field, for example `connection.serverUrl`", k => _key = k);
        Options.Add("v|value=", "The field value; if not specified, the command will print the current value", v => _value = v);
        Options.Add("c|clear", "Clear the field", _ => _clear = true);
    }

    protected override Task<int> Run()
    {
        var verb = "read";
            
        try
        {
            var config = SeqCliConfig.Read();

            if (_key != null)
            {
                if (_clear)
                {
                    verb = "clear";
                    Clear(config, _key);
                    SeqCliConfig.Write(config);
                }
                else if (_value != null)
                {
                    verb = "update";
                    Set(config, _key, _value);
                    SeqCliConfig.Write(config);
                }
                else
                {
                    Print(config, _key);
                }
            }
            else
            {
                List(config);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not {Verb} config: {ErrorMessage}", verb, Presentation.FormattedMessage(ex));
            return Task.FromResult(1);
        }
    }

    static void Print(SeqCliConfig config, string key)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var pr = ReadPairs(config).SingleOrDefault(p => p.Key == key);
        if (pr.Key == null)
            throw new ArgumentException($"Option {key} not found.");

        Console.WriteLine(Format(pr.Value));
    }
    
    /// <summary>
    /// Given an object `o = { a: { b: 2 }, c: "foo" }`
    /// can set value like
    /// * `SetDottedPath(o, "a.b", 7) => { a: { b: 7 }, c: "foo" }`
    /// * `SetDottedPath(o, "c", "cat") => { a: { b: 7 }, c: "cat" }`
    /// </summary>
    static void SetDottedPath(object o, string key, string? value)
    {
        var prefix = key.Split('.')[0];
        var nextProperty = o.GetType().GetTypeInfo().DeclaredProperties
            .Where(p => p.CanRead && p.GetMethod!.IsPublic && !p.GetMethod.IsStatic)
            .SingleOrDefault(p => Camelize(p.Name) == prefix);
        
        if (nextProperty == null)
            throw new ArgumentException($"The key ({key}) could not be found; run the command without any arguments to view all keys.");

        var isLastProperty = !key.Contains('.');
        
        if (isLastProperty)
        {
            if (nextProperty!.PropertyType.IsEnum)
            {
                if (Enum.TryParse(nextProperty!.PropertyType, value, out var enumString))
                {
                    nextProperty!.SetValue(o, enumString);
                    return;
                }
                throw new ArgumentException($"Unable to parse {value} for property {key}");
            }

            nextProperty!.SetValue(o, string.IsNullOrEmpty(value) ? null 
                : Convert.ChangeType(value, Nullable.GetUnderlyingType(nextProperty!.PropertyType) ?? nextProperty!.PropertyType, CultureInfo.InvariantCulture));
        }
        else
        {
            var rest = key.Substring(prefix.Length + 1);
            SetDottedPath(nextProperty!.GetValue(o)!, rest, value);
        }
    }

    public static void Set(SeqCliConfig config, string key, string? value)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        
        SetDottedPath(config, key, value);
    }

    static void Clear(SeqCliConfig config, string key)
    {
        Set(config, key, null);
    }

    static void List(SeqCliConfig config)
    {
        foreach (var (key, value) in ReadPairs(config))
        {
            Console.WriteLine($"{key}:");
            Console.WriteLine($"  {Format(value)}");
        }
    }

    public static IEnumerable<KeyValuePair<string, object?>> ReadPairs(object config)
    {
        foreach (var property in config.GetType().GetTypeInfo().DeclaredProperties
                     .Where(p => p.CanRead && p.GetMethod!.IsPublic && !p.GetMethod.IsStatic && !p.Name.StartsWith("Encoded"))
                     .OrderBy(p => p.Name))
        {
            var propertyName = Camelize(property.Name);
            var propertyValue = property.GetValue(config);

            if (propertyValue is IDictionary dict)
            {
                foreach (var elementKey in dict.Keys)
                {
                    foreach (var elementPair in ReadPairs(dict[elementKey]!))
                    {
                        yield return new KeyValuePair<string, object?>(
                            $"{propertyName}[{elementKey}].{elementPair.Key}",
                            elementPair.Value);
                    }
                }
            }
            else if (propertyValue?.GetType().Namespace?.StartsWith("SeqCli.Config") ?? false)
            {
                foreach (var childPair in ReadPairs(propertyValue))
                {
                    var name = propertyName + "." + childPair.Key;
                    yield return new KeyValuePair<string, object?>(name, childPair.Value);
                }
            }
            else
            {
                yield return new KeyValuePair<string, object?>(propertyName, propertyValue);
            }
        }
    }

    static string Camelize(string s)
    {
        if (s.Length < 2)
            throw new NotSupportedException("No camel-case support for short names");
        return char.ToLowerInvariant(s[0]) + s.Substring(1);
    }

    static string Format(object? value)
    {
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString() ?? "";
    }
}