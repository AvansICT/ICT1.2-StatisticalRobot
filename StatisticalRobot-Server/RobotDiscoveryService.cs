

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

                    byte[] response = Encoding.ASCII.GetBytes($"STATROBOT_V0.0_ACK_{await Utility.GetPiSerialNumber()}");
                    await client.SendAsync(response, udpResult.RemoteEndPoint, cancelToken);
                }
            }
            catch(Exception ex) 
            {
                this.logger.LogWarning(ex, "Exception occured whilst handling discovery request for client. Continueing...");
            }
        }
    }
}