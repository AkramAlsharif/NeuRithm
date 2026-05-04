# IIS Publish & Deployment Guide for Neurithm

## Prerequisites
- Windows Server or Windows 10/11 with IIS installed
- .NET 10 Hosting Bundle installed ([Download here](https://dotnet.microsoft.com/en-us/download/dotnet/10.0))
- Administrative access to IIS

## Build & Publish (Recommended)
Use the project publish script from repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-IIS.ps1
```

This script performs all required deployment steps:
- Publishes `Neurithm.Web/Neurithm.Web.csproj` to `D:\web\NeuRithm.net`
- Copies `wwwroot` assets to IIS root for correct static routing
- Writes IIS-compatible `web.config`
- Restarts IIS site `neurithm.net`
- Runs file checks for:
  - `index.html`
  - `js\microphone.js`
- Runs HTTP health checks for:
  - `/`
  - `/js/microphone.js`
- Writes deployment logs to:
  - `./logs/iis-deploy.log`
  - `D:\web\NeuRithm.net\deployment-status.log`

## Runtime Stability Note
- `InvariantGlobalization` is enabled for `Neurithm.Web` to avoid runtime dependency on ICU `.dat` downloads.
- This prevents startup failures caused by missing `icudt_*.dat` files.

## IIS Site Setup
1. Open IIS Manager.
2. Add or edit the website:
   - **Site name:** neurithm.net
   - **Physical path:** `D:\web\NeuRithm.net`
   - **Binding hostname:** `neurithm.net` (and/or `www.neurithm.net`)
   - **Port:** 80 (HTTP), 443 (HTTPS recommended)
   - **Application pool:** No Managed Code
3. Ensure site is started.

## Local Domain Setup (for development)
1. Edit hosts file (`C:\Windows\System32\drivers\etc\hosts`):
   ```
   127.0.0.1 neurithm.net
   127.0.0.1 www.neurithm.net
   ```
2. Save and close.

## HTTPS Setup (Recommended)
- Add HTTPS binding in IIS and select certificate.
- Browsers typically require HTTPS for microphone access.

## Troubleshooting
- Re-run deterministic deploy script:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\Publish-IIS.ps1
  ```
- Check logs:
  - `./logs/iis-deploy.log`
  - `D:\web\NeuRithm.net\deployment-status.log`
- If browser still uses stale JS fingerprints, hard refresh with `Ctrl+F5`.

---

For more details, see `README.md` and `COPILOT.md`.
