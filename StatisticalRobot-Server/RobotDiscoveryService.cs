

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
        // Create a UDP Listener on all ports with broadcasts enabled
        UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 9999))
        {
            EnableBroadcast = true
        };

        logger.LogInformation("Listening for discovery packets...");

        while(!cancelToken.IsCancellationRequested) 
        {
            // Receive a UDP datagram, cancel if requested
            var udpResult = await client.ReceiveAsync(cancelToken);
            if(cancelToken.IsCancellationRequested)
                break;

            try 
            {
                // Process the request
                string request = Encoding.ASCII.GetString(udpResult.Buffer);

                if(request == "STATROBOT_V0.0_DISCOV")
                {
                    this.logger.LogInformation($"Discovery from {udpResult.RemoteEndPoint}");

                    // Respond to the discovery request by using the senders remote endpoint (ip) information
                    byte[] response = Encoding.ASCII.GetBytes($"STATROBOT_V0.0_ACK_{await Utility.GetPiSerialNumber()}");
                    await client.SendAsync(response, udpResult.RemoteEndPoint, cancelToken);
                }
                else 
                {
                    this.logger.LogInformation($"Unknown/invalid request from {udpResult.RemoteEndPoint}");
                }
            }
            catch(Exception ex) 
            {
                this.logger.LogWarning(ex, "Exception occured whilst handling discovery request for client. Continueing...");
            }
        }
    }
}