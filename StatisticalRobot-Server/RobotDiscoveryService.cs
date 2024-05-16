

using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class RobotDiscoveryService : BackgroundService
{

    private readonly ILogger<RobotDiscoveryService> logger;

    private string? _piSerial;

    public RobotDiscoveryService(ILogger<RobotDiscoveryService> logger) 
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 9999));
        client.EnableBroadcast = true;

        logger.LogInformation("Listening for discovery packets...");

        while(!cancelToken.IsCancellationRequested) 
        {
            var udpResult = await client.ReceiveAsync(cancelToken);
            if(cancelToken.IsCancellationRequested)
                break;

            try 
            {
                string request = Encoding.ASCII.GetString(udpResult.Buffer);

                this.logger.LogInformation($"Discovery from {udpResult.RemoteEndPoint}, with data: {request}");

                if(request == "STATROBOT_V0.0_DISCOV")
                {
                    // int port = int.Parse(request.Substring(request.LastIndexOf('_') + 1));

                    // this.logger.LogInformation($"Sending response to {udpResult.RemoteEndPoint.Address}:{port}");

                    byte[] response = Encoding.ASCII.GetBytes($"STATROBOT_V0.0_ACK_{await this.GetPiSerialNumber()}");
                    await client.SendAsync(response, udpResult.RemoteEndPoint, cancelToken);
                }
            }
            catch(Exception ex) 
            {
                this.logger.LogWarning(ex, "Exception occured whilst handling discovery request for client. Continueing...");
            }
        }
    }

    private async Task<string> GetPiSerialNumber()
    {
        if(this._piSerial is not null)
            return this._piSerial;
        
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

        this._piSerial = !string.IsNullOrEmpty(result) ? result : "deadbeef00000000";

        return this._piSerial;
    }

}