using System.Globalization;
using System.Linq;
using SeqCli.Cli.Commands;
using SeqCli.Config;
using Serilog.Events;
using Xunit;

namespace SeqCli.Tests.Config;

public class SettingTests
{
    [Fact]
    public void CanSetPropertyOnNestedType()
    {
        var config = new SeqCliConfig();
        var targetValue = "kermit";
        ConfigCommand.Set(config, "connection.serverUrl", targetValue);
        Assert.Equal(targetValue, config.Connection.ServerUrl);
    }

    [Fact]
    public void CanSetPropertyOnDoubleNestedType()
    {
        var config = new SeqCliConfig();
        var targetValue = 1024;
        ConfigCommand.Set(config, "forwarder.storage.bufferSizeBytes", targetValue.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(targetValue, config.Forwarder.Storage.BufferSizeBytes);
    }

    [Fact]
    public void CanSetCustomTypes()
    {
        var config = new SeqCliConfig();
        LogEventLevel targetValue = LogEventLevel.Fatal;
        ConfigCommand.Set(config, "forwarder.diagnostics.internalLoggingLevel", targetValue.ToString());
        Assert.Equal(targetValue, config.Forwarder.Diagnostics.InternalLoggingLevel);
    }

    [Fact]
    public void CanSetNullableInt()
    {
        var config = new SeqCliConfig();
        uint? targetValue = null;
        ConfigCommand.Set(config, "forwarder.pooledConnectionLifetimeMilliseconds", targetValue.ToString());
        Assert.Equal(targetValue, config.Forwarder.PooledConnectionLifetimeMilliseconds);
    }
    
    [Fact]
    public void CanSetNullableIntWithValue()
    {
        var config = new SeqCliConfig();
        uint? targetValue = 8888;
        ConfigCommand.Set(config, "forwarder.pooledConnectionLifetimeMilliseconds", targetValue.ToString());
        Assert.Equal(targetValue, config.Forwarder.PooledConnectionLifetimeMilliseconds);
    }
    
    [Fact]
    public void CanSetUlong()
    {
        var config = new SeqCliConfig();

        ConfigCommand.Set(config, "forwarder.eventBodyLimitBytes", "");
        Assert.Equal(0u, config.Forwarder.EventBodyLimitBytes);
        
        ConfigCommand.Set(config, "forwarder.eventBodyLimitBytes", null);
        Assert.Equal(0u, config.Forwarder.EventBodyLimitBytes);
        
        ConfigCommand.Set(config, "forwarder.eventBodyLimitBytes", "1234");
        Assert.Equal(1234u, config.Forwarder.EventBodyLimitBytes);
    }

    [Fact]
    public void SetAllProperties()
    {
        var mutatedConfig = new SeqCliConfig();
        var kvps = ConfigCommand.ReadPairs(new SeqCliConfig());

        foreach (var (key, value) in kvps)
        {
            ConfigCommand.Set(mutatedConfig, key, value?.ToString());
        }
    }
}