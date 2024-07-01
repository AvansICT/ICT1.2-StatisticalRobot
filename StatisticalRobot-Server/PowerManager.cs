
using CliWrap;

public static class PowerManager
{

    /// <summary>
    /// Immediately shutsdown the raspberry pi
    /// </summary>
    /// <returns></returns>
    public static Task Shutdown()
    {
        return Cli.Wrap("poweroff")
            .ExecuteAsync();
    }

    /// <summary>
    /// Immediately reboots the raspberry pi
    /// </summary>
    /// <returns></returns>
    public static Task Reboot()
    {
        return Cli.Wrap("reboot")
            .ExecuteAsync();
    }

}