<#
.SYNOPSIS
	Builds Mediop and lays the result out as a deployable package.

.DESCRIPTION
	Produces artifacts/Mediop-<version>/ mirroring the Sitecore web root, plus a zip of it:

	  bin/Mediop.dll
	  App_Config/Include/Mediop/*.config
	  App_Data/Mediop Tools/**

	Copy the contents over the web root of each server, or point a web deploy at the zip. Nothing
	else is needed: the configs are already patched for their roles, and the only web.config change
	is the media handler, which is optional and only applies when next generation formats are on.

.PARAMETER Configuration
	Build configuration. Release by default.

.PARAMETER Version
	Version stamped into the folder and zip name.

.PARAMETER SkipBuild
	Package whatever is already in bin, without rebuilding.

.EXAMPLE
	.\Package.ps1 -Version 1.0.0
#>
[CmdletBinding()]
param(
	[string] $Configuration = 'Release',
	[string] $Version = '1.0.0',
	[switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$projectDir = Join-Path $root 'src/Mediop'
$solution = Join-Path $root 'Mediop.sln'
$toolsDir = Join-Path $root 'tools/Mediop Tools'
$artifacts = Join-Path $root 'artifacts'
$stage = Join-Path $artifacts "Mediop-$Version"

function Find-MSBuild {
	$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'

	if (Test-Path $vswhere) {
		$path = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
		if ($path) { return $path }
	}

	$fallback = Get-ChildItem 'C:/Program Files/Microsoft Visual Studio/*/*/MSBuild/Current/Bin/MSBuild.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
	if ($fallback) { return $fallback.FullName }

	throw 'MSBuild not found. Install Visual Studio 2022 or the Build Tools.'
}

if (-not $SkipBuild) {
	$msbuild = Find-MSBuild
	Write-Host "Building with $msbuild"

	# -restore pulls the Sitecore assemblies from the feeds in NuGet.config before compiling, so a
	# fresh clone builds in one step. It needs network access the first time; after that the packages
	# come from the local NuGet cache.
	& $msbuild $solution -restore -p:Configuration=$Configuration -v:minimal -nologo
	if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }
}

$assembly = Join-Path $projectDir "bin/$Configuration/Mediop.dll"
if (-not (Test-Path $assembly)) { throw "Built assembly not found at $assembly." }

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }

New-Item -ItemType Directory -Path (Join-Path $stage 'bin') -Force | Out-Null
Copy-Item $assembly (Join-Path $stage 'bin') -Force

$pdb = [IO.Path]::ChangeExtension($assembly, '.pdb')
if ($Configuration -eq 'Debug' -and (Test-Path $pdb)) {
	Copy-Item $pdb (Join-Path $stage 'bin') -Force
}

$configTarget = Join-Path $stage 'App_Config/Include/Mediop'
New-Item -ItemType Directory -Path $configTarget -Force | Out-Null
Copy-Item (Join-Path $projectDir 'App_Config/Include/Mediop/*') $configTarget -Recurse -Force

if (Test-Path $toolsDir) {
	$toolsTarget = Join-Path $stage 'App_Data/Mediop Tools'
	New-Item -ItemType Directory -Path $toolsTarget -Force | Out-Null
	Copy-Item "$toolsDir/*" $toolsTarget -Recurse -Force
} else {
	throw "Optimizer binaries are missing. Run Get-MediopTools.ps1 first, or pass -SkipBuild if you are packaging the assembly on its own."
}

$zip = Join-Path $artifacts "Mediop-$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$stage/*" -DestinationPath $zip

Write-Host ""
Write-Host "Package staged at $stage"
Write-Host "Zip written to     $zip"
Write-Host ""
Write-Host "Deploy by copying the contents over the web root, then recycle the app pool."
