[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $ServerName
)

# Holy crap, why am I overengineering this?

Remove-Item -Recurse ./out -Force
$RemoteDirectory = "/home/pi/home.pi.daemon"
$ExecutableName = "Home.Pi.Daemon "
$ServiceName = "home.pi.daemon.service"

# Check if service exists and is running
ssh $ServerName sudo systemctl is-active --quiet $ServiceName
if ($?) {
    Write-Host "Stopping $ServiceName on $ServerName...."
    ssh $ServerName sudo systemctl stop $ServiceName
    if (-not $?) {
        throw "Failed to stop $ServiceName on $ServerName."
    }
}
Write-Host "Building...."
dotnet publish ./src/Home.Pi.Daemon -o ./out --self-contained --runtime linux-arm64
if (-not $?) {
    exit
}

Write-Host "Deploying...."
ssh $ServerName rm -rf $RemoteDirectory `
    && ssh $ServerName mkdir $RemoteDirectory `
    && scp ./out/* $ServerName`:$RemoteDirectory/ `
    && ssh $ServerName chmod +x $RemoteDirectory/$ExecutableName `
    && ssh $ServerName chmod +x $RemoteDirectory/$ServiceName `
    && ssh $ServerName sudo ln -sf $RemoteDirectory/$ServiceName /etc/systemd/system/

if (-not $?) {
    exit
}

Write-Host "Restarting Services...."
ssh $ServerName sudo systemctl daemon-reload `
    && ssh $ServerName sudo systemctl start $ServiceName
