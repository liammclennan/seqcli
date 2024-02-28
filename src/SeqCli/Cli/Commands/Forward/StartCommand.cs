using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

[Command("forward", "start", "Start the Seq Forwarder Windows service",
    Example = "seqcli forward start")]
class StartCommand : Command
{
    public StartCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}