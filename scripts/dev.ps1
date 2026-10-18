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

function Set-GeminiKey {
    Read-Host "AHORA: ve a AI Studio, copia tu clave, vuelve a esta ventana y pulsa Enter" | Out-Null
    $k = "$(Get-Clipboard -Raw)".Trim()

    if ($k.Length -lt 20 -or $k.Contains(" ")) {
        Write-Host "El portapapeles no parece contener una clave (longitud $($k.Length)). Cópiala de nuevo y repite Set-GeminiKey." -ForegroundColor Red
        return
    }

    try {
        $models = (Invoke-RestMethod "https://generativelanguage.googleapis.com/v1beta/openai/models" -Headers @{ Authorization = "Bearer $k" }).data.id
    }
    catch {
        Write-Host "Google rechazó la clave: $($_.Exception.Message)" -ForegroundColor Red
        return
    }

    dotnet user-secrets set "Ai:ApiKey" $k --project "$global:SmartDeskRoot\src\SmartDesk.Api" | Out-Null
    Set-Clipboard -Value " "   # no dejamos la clave en el portapapeles
    Write-Host "Clave válida y guardada (empieza por $($k.Substring(0,4))..., longitud $($k.Length))." -ForegroundColor Green
    Write-Host "Modelos 'flash' disponibles:" -ForegroundColor Green
    $models | Where-Object { $_ -like "*flash*" }
}
