
using System.Data;
using System.Text;
using CliWrap;
using CliWrap.Buffered;

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

    public static async Task<string?> AddWifiConnection(string ssid, string? password) 
    {
        string? result = null;

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
        if(!nmResult.IsSuccess)
        {
            // Common error strings:
            // Password was required/password wrong => Error: Connection activation failed: Secrets were required, but not provided.
            // Password too short/password invalid characters => Error: 802-11-wireless-security.psk: property is invalid.

            string error = nmResult.StandardError;
            if(error.Contains("Error: Connection activation failed: Secrets were required, but not provided."))
            {
                if(password is null)
                {
                    result = $"A password is required to connect to '{ssid}'";
                }
                else
                {
                    result = "The password was wrong";
                }
            }
            else if (error.Contains("Error: 802-11-wireless-security.psk: property is invalid."))
            {
                result = "The password was too short or contains invalid characters";
            }
            else if(error.Contains("Error: No network with SSID")) 
            {
                result = $"The network with SSID '{ssid}' could not be found";
            }
            else 
            {
                // Throw all unknown errors
                // For all thrown exceptions, TODO: write error message like above
                throw new NotImplementedException(error);
            }
        }

        return result;
    }

    public static async Task<List<SavedWifiNetwork>> GetSavedWifiNetworksAsync()
    {
        List<SavedWifiNetworkBuilder> result = new();

        // Step 1: Get a list of all saved networks
        await Cli.Wrap("nmcli")
            .WithArguments("-t -f type,uuid,active,autoconnect c show")
            .WithStandardOutputPipe(PipeTarget.ToDelegate((line) => 
            {
                var builder = NetworkManager.ParseSavedNetworkLine(line);
                if(builder is not null)
                    result.Add(builder);
            }))
            .ExecuteAsync();

        // Step 2: Get the SSID (which is not returned by the previous command)
        List<Task> getSsidTaskList = new List<Task>(result.Count);
        foreach(var builder in result) 
        {
            getSsidTaskList.Add(
                Cli.Wrap("nmcli")
                    .WithArguments(args => 
                    {
                        args.Add("-t")
                            .Add("c")
                            .Add("show")
                            .Add(builder.Uuid!);
                    })
                    .WithStandardOutputPipe(PipeTarget.ToDelegate((line) => {
                        if(!line.StartsWith("802-11-wireless.ssid:")) // We are only interrested in the ssid line
                            return;

                        builder.SSID = line.Substring("802-11-wireless.ssid:".Length);
                    }))
                    .ExecuteAsync()
            );
        }

        // Get all ssid's parallel
        await Task.WhenAll(getSsidTaskList);

        return result.Select(b => b.Result())
            .ToList();
    }

    public static async Task ChangeActiveNetwork(Guid networkUuid) 
    {
        await Cli.Wrap("nmcli")
            .WithArguments(args => 
            {
                args.Add("c")
                    .Add("up")
                    .Add(networkUuid.ToString());
            })
            .ExecuteAsync();
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

    private static SavedWifiNetworkBuilder? ParseSavedNetworkLine(string line)
    {
        // line Format: {type:string}:{uuid:Guid}:{active:yes|no}:{autoconnect:yes|no}

        // Parse Type
        int offset = 0;
        int colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        string type = line.Substring(0, colonIdx);

        if(type != "802-11-wireless") // Filter wifi networks only
            return null;

        SavedWifiNetworkBuilder result = new SavedWifiNetworkBuilder();

        // Parse UUID
        offset = colonIdx + 1;
        colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        result.Uuid = line.Substring(offset, colonIdx - offset);

        // Parse Active
        offset = colonIdx + 1;
        colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        result.IsActive = line.Substring(offset, colonIdx - offset);

        // Parse AutoConnect
        offset = colonIdx + 1;
        result.AutoConnect = line.Substring(offset);

        return result;
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
    public string SSID {get; init;}
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

internal class SavedWifiNetwork
{
    public string SSID {get; init;}
    public Guid Uuid {get; init;}
    public bool IsActive {get; init;}
    public bool AutoConnect {get; init;}

    public SavedWifiNetwork(string ssid, Guid uuid, bool isActive, bool autoconnect) 
    {
        this.SSID = ssid;
        this.Uuid = uuid;
        this.IsActive = isActive;
        this.AutoConnect = autoconnect;
    }
}

internal class SavedWifiNetworkBuilder
{
    public string? SSID {get; set;}
    public string? Uuid {get; set;}
    public string? IsActive {get; set;}
    public string? AutoConnect {get; set;}

    public SavedWifiNetwork Result() 
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.SSID);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.Uuid);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.IsActive);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(this.AutoConnect);

        return new SavedWifiNetwork(this.SSID, Guid.Parse(this.Uuid), this.IsActive == "yes", this.AutoConnect == "yes");
    }
}