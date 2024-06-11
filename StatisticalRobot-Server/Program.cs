var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RobotDiscoveryService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RobotDiscoveryService>());

builder.Services.AddCors();

var app = builder.Build();

// Known fire and forget, shouldn't matter too much if this fails
var hotspotTask = Task.Run(() => RunBackupHotspotCheck(app.Logger));

app.UseCors(builder => 
    builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
);

app.MapGet("/test/ping", () => "pong!");

app.MapGet("/wifi/ap_list", async () =>
{
    var networkList = await NetworkManager.GetAvailableWifiNetworksAsync();
    return Results.Ok(networkList);
});

// Warning, the network connection will be lost if this endpoint was called over an wifi connection
app.MapPost("/wifi/new_connection", async (WifiNewConnectionRequestBody body) => 
{
    if(string.IsNullOrWhiteSpace(body.SSID))
        return Results.BadRequest("SSID is required but was empty");

    if(!string.IsNullOrEmpty(body.Password))
    {
        if(body.Password.Length < 8)
            return Results.BadRequest("Password length is too short, a minimal of 8 characters is required");

        if(body.Password.Any(c => c < 32 || c > 126))
            return Results.BadRequest("The specified password contains invalid characters. Use only visible, latin characters");
    }
    else
    {
        body.Password = null;
    }

    var result = await NetworkManager.AddWifiConnection(body.SSID, body.Password);

    if(string.IsNullOrEmpty(result))
        return Results.Ok();
    else
        return Results.BadRequest(result);
});

app.MapGet("/wifi/saved", async () => 
{
    var savedList = await NetworkManager.GetSavedWifiNetworksAsync();
    return Results.Ok(savedList);
});

app.MapPost("/wifi/change_active", async (WifiChangeActiveRequestBody body) => 
{
    if(body.WifiUuid == Guid.Empty)
        return Results.BadRequest("Wifi uuid is not allowed to be empty");
    
    if(await NetworkManager.ChangeActiveNetwork(body.WifiUuid))
        return Results.Ok();
    else
        return Results.BadRequest();
});

app.MapDelete("/wifi/remove_connection", async (Guid WifiUuid) => 
{
    if(WifiUuid == Guid.Empty)
        return Results.BadRequest("Wifi uuid is missing");

    if(await NetworkManager.RemoveWifiNetwork(WifiUuid))
        return Results.Ok();
    else
        return Results.BadRequest();
});

app.MapPost("/power/shutdown", async () => 
{
    await PowerManager.Shutdown();
    return Results.Ok();
});

app.MapPost("/power/reboot", async () => 
{
    await PowerManager.Reboot();
    return Results.Ok();
});

app.Run();

static async void RunBackupHotspotCheck(ILogger logger)
{
    try
    {
        System.Console.WriteLine("[Hotspot] Starting hotspot check...");

        // Quick check without scanning
        if(await NetworkManager.IsConnectedToWifiNetwork())
        {
            System.Console.WriteLine("[Hotspot] Wifi already connected!");
            return;
        }

        // Check with scanning
        if(await NetworkManager.IsConnectedToWifiNetwork(true))
        {
            System.Console.WriteLine("[Hotspot] Wifi already connected!");
            return;
        }

        System.Console.WriteLine("[Hotspot] Waiting for wifi to connect...");

        // Give the pi 10 seconds to connect
        await Task.Delay(TimeSpan.FromSeconds(10));

        System.Console.WriteLine("[Hotspot] Checking if connected to wifi...");

        // Last check, have we connected to a network after some time
        if(!await NetworkManager.IsConnectedToWifiNetwork())
        {
            System.Console.WriteLine("[Hotspot] Wifi not connected, activating backup hotspot!");

            // Start backup hotspot
            await NetworkManager.ActivateBackupHotspot();
        }
        else
        {
            System.Console.WriteLine("[Hotspot] Wifi already connected!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Hotspot] Exception occured");
    }
}