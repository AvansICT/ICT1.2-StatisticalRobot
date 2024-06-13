$workspaceFolderBasename = $args[0]
$piHost = $args[1]

dotnet build RobotProject.csproj
if (-Not $?) {
    exit -1;
}

Write-Output ""

$isIpv4Host = $piHost -match '^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$'
$isIpv6Host = $piHost -match '(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))';

if($isIpv4Host) {
    $ip = $piHost;
    $ipVersion = [System.Net.Sockets.AddressFamily]::InterNetwork;
}
elseif($isIpv6Host) {
    $ip = $piHost;
    $ipVersion = [System.Net.Sockets.AddressFamily]::InterNetworkV6;
}
else {
    $ip = "";
    $ipVersion = [System.Net.Sockets.AddressFamily]::InterNetwork;
    try {

        Write-Output "Resolving ip address for $pihost"

        $ipForPi = [System.Net.Dns]::GetHostAddresses("$piHost") | Where-Object { 
            ($_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork) -or ($_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetworkV6) 
        } | Select-Object -First 1

        $ip = $ipForPi.IPAddressToString
        $ipVersion = $ipForPi.AddressFamily

    }
    catch {
        Write-Output "Could not get ip address for $piHost"
        exit -1
    }
}

Write-Output "Testing connection with $piHost ($ip)..."
$piIsFound = Test-Connection -Quiet -Count 1 -ErrorAction SilentlyContinue $ip

if(-Not $piIsFound) {
    Write-Output "Second try testing connection with $pihost ($ip)..."
    $piIsFound = Test-Connection -Quiet -Count 4 -ErrorAction SilentlyContinue $ip
}

if (-Not $piIsFound) {
    Write-Output "Connection with $piHost ($ip) failed!"
    Write-Output "Please check if the raspberry pi is connected to the same network."
    exit -1
}
else {
    Write-Output "Connected!"
}

Write-Output "Cleaning old build files on remote..."
ssh rompi@$ip "for pid in `$(ps -ef | grep \`"RobotProject.dll\`" | grep -v \`"grep\`" | awk '{print `$2}'); do sudo kill -9 `$pid > /dev/null 2> /dev/null || :; done && mkdir -p \`"/mnt/csprojects/${workspaceFolderBasename}/\`" && rm -rf \`"/mnt/csprojects/${workspaceFolderBasename}/*\`""

if (-Not $?) {
    Write-Output "Cleaning old files failed!"
    exit -1
}

Write-Output "Deploying program to remote"

if($ipVersion -eq [System.Net.Sockets.AddressFamily]::InterNetworkV6) {
    # IPv6
    scp -6 -r "./bin/Debug/net8.0/*" "rompi@[${ip}]:'/mnt/csprojects/${workspaceFolderBasename}/'"
}
else {
    # IPv4
    scp -r "./bin/Debug/net8.0/*" "rompi@${ip}:'/mnt/csprojects/${workspaceFolderBasename}/'"
}

if (-Not $?) {
    Write-Output "Deploying program failed!"
    exit -1
}

Write-Output "Deployment Done!"