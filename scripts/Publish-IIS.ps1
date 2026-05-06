param(
    [string]$ProjectPath = "Neurithm.Web/Neurithm.Web.csproj",
    [string]$PublishPath = "D:\web\NeuRithm.net",
    [string]$SiteName = "neurithm.net"
)

$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message)

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $entry = "[$timestamp] $Message"

    $repoLogDir = Join-Path (Get-Location) "logs"
    if (-not (Test-Path $repoLogDir)) {
        New-Item -ItemType Directory -Path $repoLogDir | Out-Null
    }

    $repoLogPath = Join-Path $repoLogDir "iis-deploy.log"
    Add-Content -Path $repoLogPath -Value $entry

    $siteLogPath = Join-Path $PublishPath "deployment-status.log"
    Add-Content -Path $siteLogPath -Value $entry

    Write-Host $entry
}

function Ensure-HostsEntry {
    param([string]$HostName)

    $hostsPath = "$env:WINDIR\System32\drivers\etc\hosts"
    $entry = "127.0.0.1 $HostName"

    $current = Get-Content -Path $hostsPath -ErrorAction SilentlyContinue
    if ($current -notcontains $entry) {
        Add-Content -Path $hostsPath -Value $entry
        Write-Log "Hosts entry added: $entry"
    }
    else {
        Write-Log "Hosts entry exists: $entry"
    }
}

function Ensure-HttpsBinding {
    param(
        [string]$TargetSite,
        [string]$HostName,
        [string]$Thumbprint
    )

    $binding = Get-WebBinding -Name $TargetSite -Protocol https -HostHeader $HostName -ErrorAction SilentlyContinue
    if (-not $binding) {
        New-WebBinding -Name $TargetSite -Protocol https -Port 443 -HostHeader $HostName -SslFlags 1 | Out-Null
        Write-Log "HTTPS binding added for $HostName"
        $binding = Get-WebBinding -Name $TargetSite -Protocol https -HostHeader $HostName -ErrorAction SilentlyContinue
    }
    else {
        Write-Log "HTTPS binding exists for $HostName"
    }

    if ($null -eq $binding) {
        throw "Unable to resolve HTTPS binding for host '$HostName'."
    }

    $binding.AddSslCertificate($Thumbprint, "My")
    Write-Log "HTTPS certificate mapped for $HostName"
}

Write-Log "Starting IIS publish for Neurithm"
Write-Log "ProjectPath: $ProjectPath"
Write-Log "PublishPath: $PublishPath"

Import-Module WebAdministration

Ensure-HostsEntry -HostName "neurithm.net"
Ensure-HostsEntry -HostName "www.neurithm.net"

$cert = Get-ChildItem Cert:\LocalMachine\My |
    Where-Object { $_.Subject -eq "CN=neurithm.net" } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $cert) {
    $cert = New-SelfSignedCertificate -DnsName "neurithm.net", "www.neurithm.net" -CertStoreLocation "Cert:\LocalMachine\My" -FriendlyName "Neurithm Local HTTPS"
    Write-Log "Created self-signed HTTPS certificate: $($cert.Thumbprint)"
}
else {
    Write-Log "Using existing HTTPS certificate: $($cert.Thumbprint)"
}

Ensure-HttpsBinding -TargetSite $SiteName -HostName "neurithm.net" -Thumbprint $cert.Thumbprint
Ensure-HttpsBinding -TargetSite $SiteName -HostName "www.neurithm.net" -Thumbprint $cert.Thumbprint

dotnet publish $ProjectPath -c Release -o $PublishPath
Write-Log "dotnet publish completed"

$wwwrootPath = Join-Path $PublishPath "wwwroot"
if (Test-Path $wwwrootPath) {
    Copy-Item -Path (Join-Path $wwwrootPath "*") -Destination $PublishPath -Recurse -Force
    Write-Log "Copied wwwroot assets to IIS root"
}

$webConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <defaultDocument>
      <files>
        <clear />
        <add value="index.html" />
      </files>
    </defaultDocument>
    <staticContent>
      <remove fileExtension=".wasm" />
      <remove fileExtension=".webcil" />
      <remove fileExtension=".dat" />
      <remove fileExtension=".dll" />
      <remove fileExtension=".json" />
      <remove fileExtension=".woff" />
      <remove fileExtension=".woff2" />
      <mimeMap fileExtension=".wasm" mimeType="application/wasm" />
      <mimeMap fileExtension=".webcil" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".dat" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".dll" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <mimeMap fileExtension=".woff" mimeType="application/font-woff" />
      <mimeMap fileExtension=".woff2" mimeType="application/font-woff2" />
    </staticContent>
    <httpProtocol>
      <customHeaders>
        <remove name="Cache-Control" />
        <remove name="Pragma" />
        <remove name="Expires" />
        <add name="Cache-Control" value="no-store, no-cache, must-revalidate, max-age=0" />
        <add name="Pragma" value="no-cache" />
        <add name="Expires" value="0" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
'@

Set-Content -Path (Join-Path $PublishPath "web.config") -Value $webConfig -Encoding UTF8
Write-Log "web.config updated with static mappings and cache control"

$appCmd = "$env:windir\System32\inetsrv\appcmd.exe"
& $appCmd set vdir "$SiteName/" /physicalPath:"$PublishPath" | Out-Null
& $appCmd stop site "$SiteName" | Out-Null
& $appCmd start site "$SiteName" | Out-Null
Write-Log "IIS site restarted"

$fileChecks = @(
    "index.html",
    "_framework\icudt_EFIGS.tptq2av103.dat",
    "js\microphone.js"
)

foreach ($fileCheck in $fileChecks) {
    $fullPath = Join-Path $PublishPath $fileCheck
    if (Test-Path $fullPath) {
        Write-Log "File check PASS $fullPath"
    }
    else {
        Write-Log "File check FAIL $fullPath"
        throw "Missing file: $fullPath"
    }
}

$checks = @(
    "https://neurithm.net/",
    "https://neurithm.net/js/microphone.js",
    "https://www.neurithm.net/"
)

foreach ($url in $checks) {
    $statusCode = & curl.exe --insecure --silent --output NUL --write-out "%{http_code}" --head $url

    if ($statusCode -eq "200") {
        Write-Log "Health check PASS $url [$statusCode]"
    }
    else {
        Write-Log "Health check FAIL $url [$statusCode]"
        throw "Health check failed for $url with status $statusCode"
    }
}

Write-Log "IIS publish finished successfully"
Write-Log "IMPORTANT: trust the local certificate in your browser/OS to ensure secure-context microphone access on HTTPS"

$w3svcFolder = "C:\inetpub\logs\LogFiles\W3SVC3"
if (Test-Path $w3svcFolder) {
    $latestLog = Get-ChildItem $w3svcFolder -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -ne $latestLog) {
        Write-Log "Latest IIS log file: $($latestLog.FullName)"
        $recentErrors = Get-Content $latestLog.FullName -Tail 200 | Where-Object { $_ -match ' 404 ' -or $_ -match ' 500 ' }
        if ($recentErrors) {
            Write-Log "Recent IIS 404/500 entries (tail):"
            foreach ($entry in $recentErrors) {
                Write-Log $entry
            }
        }
        else {
            Write-Log "No recent IIS 404/500 entries found in latest log tail"
        }
    }
}
