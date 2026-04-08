param(
    [string]$Version = "2.0.0",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts/release",
    [string]$SigningThumbprint = "",
    [string]$TimestampServer = ""
)

$ErrorActionPreference = "Stop"

function Get-TrustedCodeSigningCertificate {
    param([string]$Thumbprint)

    if ([string]::IsNullOrWhiteSpace($Thumbprint)) {
        return $null
    }

    $normalized = $Thumbprint.Replace(" ", "").ToUpperInvariant()
    $certificate = Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My |
        Where-Object { $_.Thumbprint -eq $normalized } |
        Select-Object -First 1

    if ($null -eq $certificate) {
        throw "Signing certificate with thumbprint '$Thumbprint' was not found."
    }

    if (-not $certificate.HasPrivateKey) {
        throw "The signing certificate '$($certificate.Subject)' does not have an accessible private key."
    }

    $ekuValues = @($certificate.EnhancedKeyUsageList | ForEach-Object { $_.ObjectId.Value })
    if ($ekuValues.Count -gt 0 -and $ekuValues -notcontains "1.3.6.1.5.5.7.3.3") {
        throw "The certificate '$($certificate.Subject)' is not a code-signing certificate."
    }

    return $certificate
}

function Sign-FileIfRequested {
    param(
        [string]$Path,
        [string]$Thumbprint,
        [string]$TimestampServerUrl
    )

    $certificate = Get-TrustedCodeSigningCertificate -Thumbprint $Thumbprint
    if ($null -eq $certificate) {
        Write-Host "Signing skipped. No code-signing certificate thumbprint was provided."
        return $false
    }

    $signatureParams = @{
        FilePath    = $Path
        Certificate = $certificate
    }

    if (-not [string]::IsNullOrWhiteSpace($TimestampServerUrl)) {
        $signatureParams.TimestampServer = $TimestampServerUrl
    }
    else {
        Write-Warning "No timestamp server was provided. The signature will not survive certificate expiry."
    }

    $result = Set-AuthenticodeSignature @signatureParams
    if ($result.Status -ne "Valid") {
        throw "Signing failed for '$Path'. Status: $($result.Status) - $($result.StatusMessage)"
    }

    $verified = Get-AuthenticodeSignature -FilePath $Path
    if ($verified.Status -ne "Valid") {
        throw "Signature verification failed for '$Path'. Status: $($verified.Status) - $($verified.StatusMessage)"
    }

    Write-Host ("Signed: {0} | {1}" -f $Path, $verified.SignerCertificate.Subject)
    return $true
}

$projectPath = "Kritter/Kritter.csproj"
$releaseDir = $OutputRoot
$publishDir = Join-Path $releaseDir ("publish-v{0}-{1}" -f $Version, $Runtime)
$releaseExe = Join-Path $releaseDir ("Kritter-v{0}.exe" -f $Version)
$releaseZip = Join-Path $releaseDir ("Kritter-v{0}-{1}.zip" -f $Version, $Runtime)
$releaseHash = Join-Path $releaseDir ("Kritter-v{0}.exe.sha256.txt" -f $Version)

if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir | Out-Null
}

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

$publishedExe = Join-Path $publishDir "Kritter.exe"
$wasSigned = Sign-FileIfRequested -Path $publishedExe -Thumbprint $SigningThumbprint -TimestampServerUrl $TimestampServer
Copy-Item -LiteralPath $publishedExe -Destination $releaseExe -Force

$publishedPdb = Join-Path $publishDir "Kritter.pdb"
if (Test-Path $publishedPdb) {
    Remove-Item -LiteralPath $publishedPdb -Force
}

if (Test-Path $releaseZip) {
    Remove-Item -LiteralPath $releaseZip -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $releaseZip -CompressionLevel Optimal

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $releaseExe).Hash.ToLowerInvariant()
Set-Content -LiteralPath $releaseHash -Value $hash -Encoding ascii

if (-not $wasSigned) {
    Write-Warning "Release executable is unsigned. Windows SmartScreen and Smart App Control may warn users on download or launch."
}

Write-Host "Release files:"
Get-ChildItem $releaseDir | Where-Object { $_.Name -like ("Kritter-v{0}*" -f $Version) } | Select-Object Name, Length, LastWriteTime
