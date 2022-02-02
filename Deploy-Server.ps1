[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $FunctionApp
)

Push-Location -Path "./src/Home.Code.Server"

func azure functionapp publish $FunctionApp

Pop-Location