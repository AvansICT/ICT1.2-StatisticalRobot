
using System.Data;
using System.Text;
using CliWrap;
using CliWrap.Buffered;

internal class NetworkManager
{
    
    public static async Task<bool> IsConnectedToWifiNetwork(bool forceRefresh = false)
    {
        bool result = false;

        if(forceRefresh)
        {
            await Cli.Wrap("nmcli")
                .WithArguments("dev wifi rescan")
                .ExecuteAsync();

            // Rescan returns immediately, give it some time...
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        await Cli.Wrap("nmcli")
            .WithArguments("-t -f type,active c show")
            .WithStandardOutputPipe(PipeTarget.ToDelegate((line) => 
            {
                if(result)
                    return;

                // Type must be wireless
                int colonIdx = NetworkManager.GetNextColonIndex(line);
                if(line.Substring(0, colonIdx) != "802-11-wireless")
                    return;

                result = result || line.Substring(colonIdx + 1) == "yes";
            }))
            .ExecuteAsync();

        return result;
    }

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

        // The name "Hotspot" is used for the pi's backup-hotspot
        if(ssid == "Hotspot")
            return "The ssid 'Hotspot' is reserved and cannot be used";

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

        if(nmResult.IsSuccess) 
        {
            // Step 3: Change connection sessings

            string output = nmResult.StandardOutput;
            // Expected output: Device 'wlan0' successfully activated with '{wifi uuid}'.

            int uuidStopIdx = output.LastIndexOf('\'');
            if(uuidStopIdx <= 1) // The last ' could not be found or is at an invalid position (there must be enough room for a leading ')
                return "Connection settings could not be modified, could not find wifi uuid";

            int uuidStartIdx = output.LastIndexOf('\'', uuidStopIdx - 1);
            if(uuidStartIdx < 0) // The starting ' could not be found
                return "Connection settings could not be modified, could not find wifi uuid";

            string uuidStr = output.Substring(uuidStartIdx + 1, uuidStopIdx - uuidStartIdx - 1);
            if(!Guid.TryParse(uuidStr, out Guid _))
                return "Connection settings could not be modified, wifi uuid is invalid";

            nmResult = await Cli.Wrap("nmcli")
                .WithArguments(args => 
                {
                    args.Add("con")
                        .Add("modify").Add(uuidStr)
                        .Add("connection.autoconnect").Add("yes") // Enable auto connect
                        .Add("connection.autoconnect-priority").Add("100"); // Set high priority, so it will overrule hotspot connection
                })
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
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
            .WithArguments("-t -f type,name,uuid,active,autoconnect c show")
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

    public static async Task<bool> ChangeActiveNetwork(Guid networkUuid) 
    {
        var nmResult = await Cli.Wrap("nmcli")
            .WithArguments(args => 
            {
                args.Add("c")
                    .Add("up")
                    .Add(networkUuid.ToString());
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        return nmResult.IsSuccess;
    }

    public static async Task ActivateBackupHotspot()
    {
        // PiId is the serial number, excluding the leading single '1' and all 0's
        // Example: 100000BEEF => BEEF
        string piId = (await Utility.GetPiSerialNumber())
            .Substring(1)
            .TrimStart('0');

        string hotspotName = $"Robot_{piId}";
        
        bool hotspotConfigExists = false;
        await Cli.Wrap("nmcli")
            .WithArguments("-t -f name c show")
            .WithStandardOutputPipe(PipeTarget.ToDelegate((line) => 
            {
                if(line.StartsWith(hotspotName))
                    hotspotConfigExists = true;
            }))
            .ExecuteAsync();

        if(hotspotConfigExists)
        {
            // Activate existing hotspot
            await Cli.Wrap("nmcli")
                .WithArguments((args) => 
                {
                    args.Add("con")
                        .Add("up")
                        .Add(hotspotName);
                })
                .ExecuteAsync();

            // Done!
        }
        else
        {
            // Create new (will also activate immediately)
            await Cli.Wrap("nmcli")
                .WithArguments((args) => 
                {
                    args.Add("dev")
                        .Add("wifi")
                        .Add("hotspot")
                        .Add("ssid")
                        .Add(hotspotName)
                        .Add("password")
                        // I know, it's insecure, but also easy for the students
                        // The main purpose is to keep other students form other studies away
                        // The usage of the backup hotspot is also temporarly
                        .Add("avansti123");
                })
                .ExecuteAsync();
        }
    }

    public static async Task<bool> RemoveWifiNetwork(Guid networkUuid)
    {
        var nmResult = await Cli.Wrap("nmcli")
            .WithArguments(args => 
            {
                args.Add("c")
                    .Add("delete")
                    .Add(networkUuid);
            })
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        return nmResult.IsSuccess;
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

    private static SavedWifiNetworkBuilder? ParseSavedNetworkLine(string line, bool includeHotspot = false)
    {
        // line Format: {type:string}:{name:string}:{uuid:Guid}:{active:yes|no}:{autoconnect:yes|no}

        // Parse Type
        int offset = 0;
        int colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        string type = line.Substring(0, colonIdx);

        if(type != "802-11-wireless") // Filter wifi networks only
            return null;

        // Parse Name
        offset = colonIdx + 1;
        colonIdx = NetworkManager.GetNextColonIndex(line, offset);
        string name = line.Substring(offset, colonIdx - offset);

        if(!includeHotspot && name == "Hotspot") // Hide wifi-hotspot
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