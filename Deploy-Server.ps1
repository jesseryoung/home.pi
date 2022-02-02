[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $FunctionApp
)

Push-Location -Path "./src/Home.Pi.Server"

func azure functionapp publish $FunctionApp

Pop-Location