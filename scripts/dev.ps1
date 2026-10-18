# Utilidades de desarrollo de SmartDesk.
# Cargar en cada ventana nueva con:   . .\scripts\dev.ps1

$global:SmartDeskRoot = Split-Path $PSScriptRoot -Parent
$global:base = "http://localhost:5080/api/tickets"

function Stop-Api {
    Get-Process SmartDesk.Api -ErrorAction SilentlyContinue | Stop-Process -Force
}

function Start-Api {
    Stop-Api
    $cmd = "cd '$global:SmartDeskRoot'; dotnet run --project src/SmartDesk.Api --urls http://localhost:5080"
    Start-Process powershell -ArgumentList '-NoExit', '-Command', $cmd
    Write-Host "API arrancando en otra ventana. Espera a ver 'Now listening on' antes de hacer peticiones." -ForegroundColor Cyan
}

function Get-AuthHeader([string]$Name, [string]$Role) {
    $token = dotnet user-jwts create --project "$global:SmartDeskRoot\src\SmartDesk.Api" --name $Name --role $Role --output token
    @{ Authorization = "Bearer $token" }
}

function New-Ticket($Headers, [string]$Title, [string]$Description) {
    $json  = @{ title = $Title; description = $Description } | ConvertTo-Json
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    Invoke-RestMethod -Method Post -Uri $global:base -ContentType "application/json; charset=utf-8" -Headers $Headers -Body $bytes
}

function Show-HttpError([scriptblock]$Request) {
    try { & $Request; "Sin error (la petición tuvo éxito)" }
    catch {
        $resp = $_.Exception.Response
        if ($null -eq $resp) { "Sin respuesta del servidor: ¿está arrancada la API?"; return }
        "HTTP $([int]$resp.StatusCode) $($resp.StatusCode)"
        (New-Object System.IO.StreamReader($resp.GetResponseStream())).ReadToEnd()
    }
}

Write-Host "Utilidades cargadas: Start-Api, Stop-Api, Get-AuthHeader, New-Ticket, Show-HttpError" -ForegroundColor Green
