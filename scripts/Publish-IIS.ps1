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

Write-Log "Starting IIS publish for Neurithm"
Write-Log "ProjectPath: $ProjectPath"
Write-Log "PublishPath: $PublishPath"

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
        <add name="Cache-Control" value="no-cache" />
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

$baseUrl = "http://neurithm.net"
$checks = @(
    "/",
    "/_framework/icudt_EFIGS.tptq2av103.dat",
    "/js/microphone.js"
)

foreach ($path in $checks) {
    $url = "$baseUrl$path"
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Head
        Write-Log "Health check PASS $url [$($response.StatusCode)]"
    }
    catch {
        $statusCode = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { "N/A" }
        Write-Log "Health check FAIL $url [$statusCode]"
        throw
    }
}

Write-Log "IIS publish finished successfully"
