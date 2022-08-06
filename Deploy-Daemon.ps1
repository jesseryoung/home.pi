[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $ServerName,
    [Parameter()]
    [string]
    $User = "pi"
)

# Holy crap, why am I overengineering this?

Remove-Item -Recurse ./out -Force
$RemoteDirectory = "/home/pi/home.pi.daemon"
$ExecutableName = "Home.Pi.Daemon "
$ServiceName = "home.pi.daemon.service"

$serverAddress = [System.Net.Dns]::GetHostByName($ServerName).AddressList[0].IPAddressToString

# Check if service exists and is running
ssh $User@$serverAddress sudo systemctl is-active --quiet $ServiceName
if ($?) {
    Write-Host "Stopping $ServiceName on $serverAddress...."
    ssh $User@$serverAddress sudo systemctl stop $ServiceName
    if (-not $?) {
        throw "Failed to stop $ServiceName on $serverAddress."
    }
}
Write-Host "Building...."
dotnet publish ./src/Home.Pi.Daemon -o ./out --self-contained --runtime linux-arm64
if (-not $?) {
    exit
}

Write-Host "Deploying...."
ssh $User@$serverAddress rm -rf $RemoteDirectory `
    && ssh $User@$serverAddress mkdir $RemoteDirectory `
    && scp -o user=$User ./out/* $serverAddress`:$RemoteDirectory/ `
    && ssh $User@$serverAddress chmod +x $RemoteDirectory/$ExecutableName `
    && ssh $User@$serverAddress chmod +x $RemoteDirectory/$ServiceName `
    && ssh $User@$serverAddress sudo ln -sf $RemoteDirectory/$ServiceName /etc/systemd/system/

if (-not $?) {
    exit
}

Write-Host "Restarting Services...."
ssh $User@$serverAddress sudo systemctl daemon-reload `
    && ssh $User@$serverAddress sudo systemctl start $ServiceName
