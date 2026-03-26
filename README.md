# Kritter

Kritter is a Windows desktop tool built with WPF and .NET 8 for post-format setup, software reinstallation, and system optimization.

It helps you prepare a reusable `.kritter` package from an existing machine, then apply that package after a fresh Windows installation. The app can reinstall supported software with `winget` or direct installers, bundle local setup files into the package, run curated optimization scripts, and continue the workflow after a restart.

## What It Does

- Scans the current system for reinstallable applications
- Creates portable `.kritter` packages for later use
- Reinstalls apps with `winget` or silent direct installers
- Detects local setup files and includes them in the package
- Applies optimization scripts from `Kritter Recommended`
- Supports optional script selection from `fr33ty Recommended`
- Resumes the installation flow after a reboot
- Runs a cleanup phase after setup is completed

## Project Structure

- `Kritter/`: WPF application source code
- `Kritter Recommended/`: built-in recommended optimization scripts
- `fr33ty Recommended/`: additional optimization script collection

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK for building from source
- `winget` available on the target system
- Administrator privileges for some optimization and setup steps

## Build

```powershell
dotnet build Kritter.sln
```

## Publish

```powershell
dotnet publish Kritter\\Kritter.csproj -c Release -r win-x64 --self-contained false
```

## Notes

- The UI is currently in Turkish.
- Optimization scripts modify Windows settings. Creating a restore point before use is recommended.
- Build outputs are intentionally excluded from source control.
