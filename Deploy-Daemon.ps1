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

Write-Host "Building...."
dotnet publish ./src/Home.Pi.Daemon -o ./out --self-contained --runtime linux-arm64
if (-not $?) {
    exit
}

$serverAddress = [System.Net.Dns]::GetHostByName($ServerName).AddressList[0].IPAddressToString
foreach($service in Get-ChildItem ./out/systemd)
{
    $serviceName = $service.Name
    # Check if service exists and is running
    ssh $User@$serverAddress sudo systemctl is-active --quiet $serviceName
    if ($?) {
        Write-Host "Stopping $serviceName on $serverAddress...."
        ssh $User@$serverAddress sudo systemctl stop $serviceName
        if (-not $?) {
            throw "Failed to stop $serviceName on $serverAddress."
        }
    }
}


Write-Host "Deploying...."
ssh $User@$serverAddress rm -rf $RemoteDirectory `
    && ssh $User@$serverAddress mkdir $RemoteDirectory `
    && scp -r -o user=$User ./out/* $serverAddress`:$RemoteDirectory/ `
    && ssh $User@$serverAddress chmod +x $RemoteDirectory/$ExecutableName `
    && ssh $User@$serverAddress chmod +x $RemoteDirectory/systemd/*.service `
    && ssh $User@$serverAddress sudo ln -sf $RemoteDirectory/systemd/*.service /etc/systemd/system/

if (-not $?) {
    exit
}


ssh $User@$serverAddress sudo systemctl daemon-reload
if (-not $?) {
    throw "Failed to reload services $serverAddress."
}
foreach($service in Get-ChildItem ./out/systemd)
{
    $serviceName = $service.Name
    Write-Host "Restarting $serviceName...."
    ssh $User@$serverAddress sudo systemctl start $serviceName
    if (-not $?) {
        throw "Failed to restart $serviceName on $serverAddress."
    }
}
