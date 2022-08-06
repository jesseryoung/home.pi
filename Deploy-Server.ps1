[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $FunctionApp
)

Push-Location -Path "./src/Home.Pi.Server"

func azure functionapp publish $FunctionApp
az functionapp config set --name azfun-home --resource-group rg-home --linux-fx-version 'dotnet-isolated|7.0' | Out-Null

Pop-Location