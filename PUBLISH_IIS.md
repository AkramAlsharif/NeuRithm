# IIS Publish & Deployment Guide for Neurithm

## Prerequisites
- Windows Server or Windows 10/11 with IIS installed
- .NET 10 Hosting Bundle installed ([Download here](https://dotnet.microsoft.com/en-us/download/dotnet/10.0))
- Administrative access to IIS

## Build & Publish
1. Open a terminal in your project root:
   ```powershell
   dotnet publish src/Neurithm.Web/Neurithm.Web.csproj -c Release -o ./publish
   ```
2. The output will be in the `publish` folder.

## IIS Site Setup
1. Open IIS Manager.
2. Add a new Website:
   - **Site name:** Neurithm
   - **Physical path:** `D:\web\NeuRithm\publish`
   - **Binding hostname:** `neurithm.net` (and/or `www.neurithm.net`)
   - **Port:** 80 (HTTP), 443 (HTTPS, recommended)
   - **Application pool:** No Managed Code
3. Ensure `web.config` is present in the publish folder.
4. Start the site.

## Local Domain Setup (for development)
1. Edit your hosts file (`C:\Windows\System32\drivers\etc\hosts`):
   ```
   127.0.0.1 neurithm.net
   127.0.0.1 www.neurithm.net
   ```
2. Save and close the file.

## HTTPS Setup (Recommended)
- Add an HTTPS binding in IIS and select a certificate.
- For local development, you can use a self-signed certificate.
- Browsers require HTTPS for microphone access.

## Environment & Configuration
- Use `appsettings.json` for environment, file storage root, game settings, and audio settings.
- Example:
  ```json
  {
    "FileStorageOptions": {
      "RootPath": "App_Data"
    },
    "AudioOptions": {
      "A4Tuning": 440,
      "MicSensitivity": "Normal"
    }
  }
  ```

## Publish Command
- To publish for IIS:
  ```powershell
  dotnet publish -c Release -o ./publish
  ```

## Troubleshooting
- If you see a 500 error, check the Windows Event Viewer and the `logs` folder in your publish directory.
- Make sure the .NET Hosting Bundle is installed on the IIS server.
- Ensure the Application Pool is set to **No Managed Code**.

---

For more details, see the README and COPILOT.md.
