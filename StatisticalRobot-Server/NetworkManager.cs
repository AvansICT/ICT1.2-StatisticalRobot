
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using OneOf;
using OneOf.Types;

internal class NetworkManager
{
    
    public static async Task<List<AvailableWifiNetwork>> GetAvailableWifiNetworksAsync() 
    {
        List<AvailableWifiNetwork> result = new();

        await Cli.Wrap("nmcli")
            .WithArguments("-f ssid,in-use,signal,security -t dev wifi list --rescan yes")
            .WithStandardOutputPipe(PipeTarget.ToDelegate((line) => result.Add(NetworkManager.ParseAvailableNetworkLine(line))))
            .ExecuteAsync();

        result.Sort((a, b) => b.SignalQuality.CompareTo(a.SignalQuality));

        return result.DistinctBy(x => x.SSID)
            .ToList();
    }

    public static async Task<OneOf<bool, string>> AddWifiConnection(string ssid, string? password) 
    {
        // Step 1: Search for ssid
        await Cli.Wrap("nmcli")
            .WithArguments(args => 
            {
                args.Add("dev")
                    .Add("wifi")
                    .Add("rescan")
                    .Add("ssid")
                    .Add(ssid);
            })
            .ExecuteAsync();

        // Step 2: Connect to network
        var nmResult = await Cli.Wrap("nmcli")
            .WithArguments(args => 
            {
                args.Add("-t")
                    .Add("dev")
                    .Add("wifi")
                    .Add("connect")
                    .Add(ssid);

                if(password is not null) 
                {
                    args.Add("password")
                        .Add(password);
                }
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        // Step 3: Parse results
        if(nmResult.IsSuccess) 
        {
            return true;
        }
        else
        {
            // Common error strings:
            // Password was required/password wrong => Error: Connection activation failed: Secrets were required, but not provided.
            // Password too short/password invalid characters => Error: 802-11-wireless-security.psk: property is invalid.

            string error = nmResult.StandardError;
            if(error.Contains("Error: Connection activation failed: Secrets were required, but not provided."))
            {
                if(password is null)
                {
                    return $"A password is required to connect to '{ssid}'";
                }
                else
                {
                    return "The password was wrong";
                }
            }
            else if (error.Contains("Error: 802-11-wireless-security.psk: property is invalid."))
            {
                return "The password was too short or contains invalid characters";
            }
            else if(error.Contains("Error: No network with SSID")) 
            {
                return $"The network with SSID '{ssid}' could not be found";
            }
            else 
            {
                // Throw all unknown errors
                // For all thrown exceptions, TODO: write error message like above
                throw new NotImplementedException(error);
            }
        }
    }

    private static AvailableWifiNetwork ParseAvailableNetworkLine(string line)
    {
        // line Format: {ssid:string}:{in-use:' '|'*'}:{signal:int}:{security:string[], space seperated}

        // Parse SSID
        int offset = 0;
        int colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        string ssid = line.Substring(0, colonIdx);

        if(string.IsNullOrEmpty(ssid))
            ssid = "(hidden)";

        // Parse InUse
        offset = colonIdx + 1;
        colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        string inUseStr = line.Substring(offset, colonIdx - offset);
        bool inUse = inUseStr == "*";

        // Parse Signal
        offset = colonIdx + 1;
        colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        string signalStr = line.Substring(offset, colonIdx - offset);
        int signal = int.Parse(signalStr);

        // Parse Security
        offset = colonIdx + 1;
        string? security = line.Substring(offset);

        if(string.IsNullOrWhiteSpace(security) || security.Contains("--"))
            security = "Open";

        return new AvailableWifiNetwork(ssid, inUse, signal, security);
    }

    private static int GetNextColonIndex(string str, int startIndex = 0) 
    {
        int colonIdx = str.IndexOf(':', startIndex);

        // Check if colon is escaped: prefixed with \
        if(colonIdx > 0 && str[colonIdx - 1] == '\\')
            return NetworkManager.GetNextColonIndex(str, colonIdx + 1);

        if(colonIdx < 0)
            throw new FormatException($"No remaining ':' found from startIndex {startIndex} in the line: {str}");

        return colonIdx;
    }

}

internal class AvailableWifiNetwork
{
    public string SSID {get;}
    public bool InUse {get; init;}

    public int SignalQuality {get; init;}

    public string? SecurityProtocol {get; init;} = null;

    public AvailableWifiNetwork(string ssid, bool inUse, int signalQuality, string? securityProtocol) 
    {
        this.SSID = ssid;
        this.InUse = inUse;
        this.SignalQuality = signalQuality;
        this.SecurityProtocol = securityProtocol;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append(this.SSID);

        if(this.InUse)
            sb.Append(" (used)");

        sb.Append(": ");
        sb.Append(this.SecurityProtocol ?? "Open");

        sb.Append(", Quality: ");
        sb.Append(this.SignalQuality);
        sb.Append('%');

        return sb.ToString();
    }
}