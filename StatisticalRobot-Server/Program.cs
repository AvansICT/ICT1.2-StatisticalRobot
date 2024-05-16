var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

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


app.Run();