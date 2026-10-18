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
    Write-Host "Arrancando la API en otra ventana..." -ForegroundColor Cyan
    for ($i = 0; $i -lt 90; $i++) {
        Start-Sleep -Seconds 1
        try {
            Invoke-RestMethod "http://localhost:5080/" -TimeoutSec 2 | Out-Null
            Write-Host "API lista ($($i + 1) s)." -ForegroundColor Green
            return
        } catch { }
    }
    Write-Host "La API no respondió en 90 s. Revisa su ventana por si hay un error." -ForegroundColor Red
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

function Set-GeminiKey {
    Write-Host ""
    Write-Host "  1. Ve al navegador, a https://aistudio.google.com/apikey" -ForegroundColor Yellow
    Write-Host "  2. Pulsa el boton de COPIAR de tu clave" -ForegroundColor Yellow
    Write-Host "  3. Vuelve aqui y pulsa Enter (NO pegues nada)" -ForegroundColor Yellow
    Read-Host "Pulsa Enter cuando hayas copiado la clave" | Out-Null
    $k = "$(Get-Clipboard -Raw)".Trim()

    if ($k.Length -lt 20 -or $k.Contains(" ")) {
        Write-Host "El portapapeles no contiene una clave (longitud $($k.Length)). Repite Set-GeminiKey y copia la clave en el paso 2." -ForegroundColor Red
        return
    }

    try {
        $models = (Invoke-RestMethod "https://generativelanguage.googleapis.com/v1beta/openai/models" -Headers @{ Authorization = "Bearer $k" }).data.id
    }
    catch {
        Write-Host "Google rechazo la clave: $($_.Exception.Message)" -ForegroundColor Red
        return
    }

    dotnet user-secrets set "Ai:ApiKey" $k --project "$global:SmartDeskRoot\src\SmartDesk.Api" | Out-Null
    Set-Clipboard -Value " "
    Write-Host "Clave valida y guardada (empieza por $($k.Substring(0,4))..., longitud $($k.Length), $($models.Count) modelos disponibles)." -ForegroundColor Green
}

Write-Host "Utilidades cargadas: Start-Api, Stop-Api, Get-AuthHeader, New-Ticket, Show-HttpError, Set-GeminiKey" -ForegroundColor Green
