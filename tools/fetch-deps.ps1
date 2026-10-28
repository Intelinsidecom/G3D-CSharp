$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent $PSScriptRoot
$lib = Join-Path $root 'lib'
$cache = Join-Path $env:TEMP 'g3d6-deps'

New-Item -ItemType Directory -Force -Path $lib | Out-Null
New-Item -ItemType Directory -Force -Path $cache | Out-Null

function Get-NuGetPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Version
    )

    $lower = $Id.ToLowerInvariant()
    $nupkg = Join-Path $cache "$lower.$version.nupkg"
    $destination = Join-Path $cache "$lower.$version"

    if (-not (Test-Path -LiteralPath $nupkg)) {
        $uri = "https://api.nuget.org/v3-flatcontainer/$lower/$version/$lower.$version.nupkg"
        Invoke-WebRequest -Uri $uri -OutFile $nupkg -UseBasicParsing
    }

    if (Test-Path -LiteralPath $destination) {
        Remove-Item -LiteralPath $destination -Recurse -Force
    }

    [System.IO.Compression.ZipFile]::ExtractToDirectory($nupkg, $destination)
    return $destination
}

function Copy-PackageAssembly {
    param(
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$FileName
    )

    $source = Join-Path $PackageDirectory $RelativePath
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Missing assembly: $source"
    }

    Copy-Item -LiteralPath $source -Destination (Join-Path $lib $FileName) -Force
}

$openTk = Get-NuGetPackage -Id 'OpenTK' -Version '3.1.0'
Copy-PackageAssembly -PackageDirectory $openTk -RelativePath 'lib\net20\OpenTK.dll' -FileName 'OpenTK.dll'

$openTkGlControl = Get-NuGetPackage -Id 'OpenTK.GLControl' -Version '3.1.0'
Copy-PackageAssembly -PackageDirectory $openTkGlControl -RelativePath 'lib\net20\OpenTK.GLControl.dll' -FileName 'OpenTK.GLControl.dll'

$sharpDx = Get-NuGetPackage -Id 'SharpDX' -Version '4.2.0'
Copy-PackageAssembly -PackageDirectory $sharpDx -RelativePath 'lib\net40\SharpDX.dll' -FileName 'SharpDX.dll'

$sharpDxDxgi = Get-NuGetPackage -Id 'SharpDX.DXGI' -Version '4.2.0'
Copy-PackageAssembly -PackageDirectory $sharpDxDxgi -RelativePath 'lib\net40\SharpDX.DXGI.dll' -FileName 'SharpDX.DXGI.dll'

$sharpDxD3D11 = Get-NuGetPackage -Id 'SharpDX.Direct3D11' -Version '4.2.0'
Copy-PackageAssembly -PackageDirectory $sharpDxD3D11 -RelativePath 'lib\net40\SharpDX.Direct3D11.dll' -FileName 'SharpDX.Direct3D11.dll'

$sharpDxD3DCompiler = Get-NuGetPackage -Id 'SharpDX.D3DCompiler' -Version '4.2.0'
Copy-PackageAssembly -PackageDirectory $sharpDxD3DCompiler -RelativePath 'lib\net40\SharpDX.D3DCompiler.dll' -FileName 'SharpDX.D3DCompiler.dll'

Get-ChildItem -LiteralPath $lib -Filter '*.dll' | Select-Object Name, Length
