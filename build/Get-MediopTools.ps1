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

.PARAMETER GifsicleUrl
	Where to fetch the gifsicle zip from. Point this at an internal mirror when the public site is
	unreachable, which on a corporate network is usually a TLS trust problem rather than an outage.

.PARAMETER SkipGifsicle
	Do not fetch gifsicle at all. Use this when the site serves no GIFs; also delete
	Mediop.Gif.config so it does not log an error per GIF.

.EXAMPLE
	.\Get-MediopTools.ps1

.EXAMPLE
	.\Get-MediopTools.ps1 -GifsicleUrl "https://artifacts.internal/mirror/gifsicle-1.95-win64.zip"

.EXAMPLE
	.\Get-MediopTools.ps1 -SkipGifsicle
#>
[CmdletBinding()]
param(
	[switch] $Force,
	[string] $GifsicleUrl = 'https://eternallybored.org/misc/gifsicle/releases/gifsicle-1.95-win64.zip',
	[switch] $SkipGifsicle
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

# SHA-256 of gifsicle.exe from the 1.95 win64 release, so a mirrored or hand-placed copy can be
# checked. A mismatch is reported but not treated as fatal - a different release is a fair reason.
$gifsicleExeSha256 = '6F60CC7F696AB4B861BF9E6FB5B4FD940B3CB6B9731E2EF04708334AF95A7DE4'

if ($SkipGifsicle) {
	Write-Host 'Skipping gifsicle (-SkipGifsicle). Delete Mediop.Gif.config if the site serves no GIFs.'
} elseif ((Test-Path $gifsicleExe) -and -not $Force) {
	$skipped++
} else {
	$zipPath = Join-Path $env:TEMP "gifsicle-$([guid]::NewGuid()).zip"

	try {
		Write-Host 'Downloading gifsicle/gifsicle.exe'
		Invoke-WebRequest -Uri $GifsicleUrl -OutFile $zipPath -UseBasicParsing

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

		$actualSha = (Get-FileHash $gifsicleExe -Algorithm SHA256).Hash
		if ($actualSha -ne $gifsicleExeSha256) {
			Write-Warning "gifsicle.exe SHA-256 is $actualSha, expected $gifsicleExeSha256."
			Write-Warning 'That is fine if you deliberately mirrored a different release; investigate otherwise.'
		}

		$downloaded++
	} catch {
		# not fatal: every other optimizer still works, only GIF support is missing
		Write-Warning "Could not fetch gifsicle: $($_.Exception.Message)"
		Write-Warning ''
		Write-Warning 'On a corporate network this is usually TLS trust, not an outage. The host uses a'
		Write-Warning 'Lets Encrypt certificate chaining to ISRG Root X2, which is missing on Windows'
		Write-Warning 'machines where automatic root certificate updates are disabled by policy, and an'
		Write-Warning 'inspecting proxy can break the chain too. Three ways forward:'
		Write-Warning ''
		Write-Warning '  1. Mirror the zip internally and rerun with -GifsicleUrl <url>'
		Write-Warning "  2. Download it by hand from https://eternallybored.org/misc/gifsicle/ and put"
		Write-Warning "     gifsicle.exe in $gifsicleDir"
		Write-Warning "     Expected SHA-256: $gifsicleExeSha256"
		Write-Warning '  3. Rerun with -SkipGifsicle and delete Mediop.Gif.config if the site has no GIFs'
	} finally {
		if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
	}
}

Write-Host ""
Write-Host "$downloaded downloaded, $skipped already present, in $targetRoot"
