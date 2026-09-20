<#
.SYNOPSIS
	Downloads the image optimization executables into tools/Mediop Tools.

.DESCRIPTION
	The optimizers shell out to native Windows binaries. They are not committed to this repository -
	they are third party builds of several MB each, under their own licenses - so this script pulls
	them from the Dianoga project, which redistributes them with the required license files.

	Run it once after cloning, and again whenever the tool set changes. Package.ps1 will refuse to
	build a deployment package until the binaries are present.

.PARAMETER Force
	Re-download files that already exist.

.EXAMPLE
	.\Get-MediopTools.ps1
#>
[CmdletBinding()]
param(
	[switch] $Force
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$baseUrl = 'https://raw.githubusercontent.com/kamsar/Dianoga/master/src/Dianoga/Dianoga Tools'
$targetRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'tools/Mediop Tools'

# Each entry is a path relative to both the upstream folder and the local tools folder.
$files = @(
	'mozjpeg/cjpeg.exe'
	'mozjpeg/jpegtran.exe'
	'mozjpeg/LICENSE.txt'
	'pngquant/pngquant.exe'
	'pngquant/COPYRIGHT'
	'pngquant/README.txt'
	'PNGOptimizer/PngOptimizerCL.exe'
	'PNGOptimizer/License.txt'
	'PNGOptimizer/Readme.txt'
	'SVGO/svgo-win.exe'
	'SVGO/LICENSE.txt'
	'SVGO/svgo.sample.config.js'
	'libwebp/cwebp.exe'
	'libwebp/gif2webp.exe'
	'libwebp/COPYING.txt'
	'avif/avifenc.exe'
	'avif/LICENSE.txt'
	'jxl/cjxl.exe'
	'jxl/LICENSE.libjxl'
	'jpegoptim-windows/jpegoptim.exe'
	'jpegoptim-windows/COPYING.txt'
)

# gifsicle comes from its own site; handled separately below.

$downloaded = 0
$skipped = 0

foreach ($file in $files) {
	$targetPath = Join-Path $targetRoot $file
	$targetDir = Split-Path $targetPath -Parent

	if ((Test-Path $targetPath) -and -not $Force) {
		$skipped++
		continue
	}

	if (-not (Test-Path $targetDir)) {
		New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
	}

	$url = "$baseUrl/$file" -replace ' ', '%20'

	Write-Host "Downloading $file"
	Invoke-WebRequest -Uri $url -OutFile $targetPath -UseBasicParsing
	$downloaded++
}

# gifsicle ships as a zip from its own site rather than as loose files, so it needs its own step.
$gifsicleDir = Join-Path $targetRoot 'gifsicle'
$gifsicleExe = Join-Path $gifsicleDir 'gifsicle.exe'

if ((Test-Path $gifsicleExe) -and -not $Force) {
	$skipped++
} else {
	$gifsicleUrl = 'https://eternallybored.org/misc/gifsicle/releases/gifsicle-1.95-win64.zip'
	$zipPath = Join-Path $env:TEMP "gifsicle-$([guid]::NewGuid()).zip"

	try {
		Write-Host 'Downloading gifsicle/gifsicle.exe'
		Invoke-WebRequest -Uri $gifsicleUrl -OutFile $zipPath -UseBasicParsing

		if (-not (Test-Path $gifsicleDir)) {
			New-Item -ItemType Directory -Path $gifsicleDir -Force | Out-Null
		}

		Add-Type -AssemblyName System.IO.Compression.FileSystem
		$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)

		try {
			# only the binary and its license; the archive also carries gifdiff and HTML manpages
			foreach ($wanted in @(@{ Entry = 'gifsicle.exe'; As = 'gifsicle.exe' },
			                      @{ Entry = 'doc/COPYING';  As = 'COPYING.txt'  })) {
				$entry = $zip.Entries | Where-Object { $_.FullName -eq $wanted.Entry }

				if ($null -eq $entry) {
					throw "The gifsicle archive does not contain $($wanted.Entry). The release layout may have changed."
				}

				[IO.Compression.ZipFileExtensions]::ExtractToFile(
					$entry, (Join-Path $gifsicleDir $wanted.As), $true)
			}
		} finally {
			$zip.Dispose()
		}

		$downloaded++
	} catch {
		# not fatal: every other optimizer still works, only GIF support is missing
		Write-Warning "Could not fetch gifsicle: $($_.Exception.Message)"
		Write-Warning "Download it manually from https://eternallybored.org/misc/gifsicle/ into $gifsicleDir,"
		Write-Warning 'or delete Mediop.Gif.config so it does not log an error per GIF.'
	} finally {
		if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
	}
}

Write-Host ""
Write-Host "$downloaded downloaded, $skipped already present, in $targetRoot"
