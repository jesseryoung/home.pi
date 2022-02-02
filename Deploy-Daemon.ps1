[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $ServerName
)

Remove-Item -Recurse ./out -Force

dotnet publish ./src/Home.Code.Daemon -o ./out --self-contained `
    && ssh $ServerName rm -rf /home/pi/home.code.daemon `
    && ssh $ServerName mkdir /home/pi/home.code.daemon `
    && scp ./out/* $ServerName`:/home/pi/home.code.daemon/ `
    && ssh $ServerName chmod +x /home/pi/home.code.daemon/Home.Code.Daemon