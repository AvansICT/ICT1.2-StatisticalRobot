
public static class Utility
{
    private static string? _piSerial = null;

    /// <summary>
    /// Gets the pi's serial number
    /// </summary>
    /// <returns>The serial number or '10000000deadbeef' if retrieving failed</returns>
    public static async Task<string> GetPiSerialNumber()
    {
        if(Utility._piSerial is not null)
            return Utility._piSerial;
        
        string? result = null;

        using var reader = new StreamReader(new FileInfo("/proc/cpuinfo").OpenRead());
        string? line;
        while((line = await reader.ReadLineAsync()) is not null)
        {
            if(!line.StartsWith("Serial"))
                continue;

            int spaceIdx = line.LastIndexOf(' ');
            if(spaceIdx < 0)
                break;

            result = line.Substring(spaceIdx + 1);
            break;
        }

        Utility._piSerial = !string.IsNullOrEmpty(result) ? result : "10000000deadbeef";
        return Utility._piSerial;
    }
}