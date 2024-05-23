
using CliWrap;

public static class PowerManager
{

    public static Task Shutdown()
    {
        return Cli.Wrap("poweroff")
            .ExecuteAsync();
    }

    public static Task Reboot()
    {
        return Cli.Wrap("reboot")
            .ExecuteAsync();
    }

}