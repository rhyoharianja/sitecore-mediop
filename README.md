# Sitecore-Mediop

[![Sitecore](https://img.shields.io/badge/Sitecore-10.4.1-EB1F23?logo=sitecore&logoColor=white)](https://doc.sitecore.com/xp/en/developers/104/sitecore-experience-manager/index-en.html)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![C#](https://img.shields.io/badge/C%23-7.3-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![PowerShell](https://img.shields.io/badge/PowerShell-5.1+-5391FE?logo=powershell&logoColor=white)](https://learn.microsoft.com/powershell/)
[![SXA](https://img.shields.io/badge/SXA-compatible-6E4C9E)](#next-generation-formats-webp-avif-jpeg-xl)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

Automatic media library image optimization for Sitecore. Every image served from the media library
(JPEG, PNG, GIF, SVG, and optionally WebP/AVIF/JPEG XL) is compressed on its way into the media
cache, with no changes to front end code or content items.

Built for **Sitecore XM/XP 10.4.1**, .NET Framework 4.8. Assembly and namespace are both `Mediop`.

Licensed under [MIT](LICENSE). Third party components and their licenses: [NOTICE.md](NOTICE.md).
The reasoning behind the technical choices: [Design notes](#design-notes).

### Optimizers

[![mozjpeg](https://img.shields.io/badge/JPEG-mozjpeg-orange)](https://github.com/mozilla/mozjpeg)
[![pngquant](https://img.shields.io/badge/PNG-pngquant%20%2B%20PNGOptimizer-blue)](https://pngquant.org/)
[![gifsicle](https://img.shields.io/badge/GIF-gifsicle-green)](https://www.lcdf.org/gifsicle/)
[![SVGO](https://img.shields.io/badge/SVG-SVGO-yellowgreen)](https://github.com/svg/svgo)
[![WebP](https://img.shields.io/badge/WebP-cwebp-brightgreen)](https://developers.google.com/speed/webp)
[![AVIF](https://img.shields.io/badge/AVIF-avifenc-lightgrey)](https://github.com/AOMediaCodec/libavif)
[![JPEG XL](https://img.shields.io/badge/JPEG%20XL-cjxl-lightgrey)](https://github.com/libjxl/libjxl)

### Support this project

Mediop is free and MIT licensed — use it in commercial work, fork it, ship it, no strings attached.
It is built and maintained in spare time, so if it cut your media payload down and saved you some
bandwidth bills, buying a coffee is a genuine help. It goes towards keeping the optimizer binaries
current and the module tested against new Sitecore releases.

[![PayPal](https://img.shields.io/badge/PayPal-Support%20via%20PayPal-00457C?logo=paypal&logoColor=white)](https://www.paypal.com/paypalme/sgkharianja)
[![Saweria](https://img.shields.io/badge/Saweria-Traktir%20Kopi-FAB702?logo=buymeacoffee&logoColor=black)](https://saweria.co/rhioharianja)

Either link works: [PayPal](https://www.paypal.com/paypalme/sgkharianja) for international,
[Saweria](https://saweria.co/rhioharianja) if you are in Indonesia. Starring the repository costs
nothing and helps just as much.

---

## How it works

1. Sitecore serves a media request and produces a stream, already resized per `?w=`/`?h=`.
2. The `mediopOptimize` pipeline decides whether that stream is worth optimizing, then dispatches it
   to a format specific pipeline (`mediopOptimizeJpeg`, `mediopOptimizePng`, and so on).
3. Each format pipeline runs one or more optimizers. An optimizer shells out to a native binary
   (cjpeg, pngquant, cwebp, ...) through temp files.
4. The result goes into the media cache, so later requests do no work at all.

If optimization fails, produces a larger file, or produces the exact same size, the result is
discarded and the original bytes are used. There is no path by which a corrupted image reaches a
browser.

---

## Layout

```
sitecore-mediop/
├─ Mediop.sln
├─ NuGet.config                      # nuget.org + the public Sitecore feed
├─ src/Mediop/
│  ├─ Mediop.csproj               # net48, PackageReference to Sitecore 10.4.1 assemblies
│  ├─ MediaOptimizer.cs           # entry point: runs the mediopOptimize pipeline
│  ├─ Processors/                 # guards + per extension dispatcher
│  ├─ Optimizers/                 # base classes + one implementation per tool
│  ├─ Invokers/                   # sync (getMediaStream) and async (media cache) strategies
│  ├─ NextGenFormats/             # WebP/AVIF/JXL negotiation via the Accept header
│  ├─ Svg/                        # SVG specific handling
│  └─ App_Config/Include/Mediop/  # every .config file
├─ build/
│  ├─ Get-MediopTools.ps1         # downloads the optimizer binaries
│  └─ Package.ps1                 # build + lay out a deployable package
└─ tools/Mediop Tools/            # downloaded binaries (not committed)
```

---

## Build

```powershell
cd sitecore-mediop
.\build\Get-MediopTools.ps1           # once after cloning
.\build\Package.ps1 -Version 1.0.0
```

Output lands in `artifacts/`:

```
artifacts/Mediop-1.0.0/
├─ bin/Mediop.dll
├─ App_Config/Include/Mediop/*.config
└─ App_Data/Mediop Tools/**
artifacts/Mediop-1.0.0.zip
```

The project stands on its own — it does not depend on another solution or a shared `packages`
folder. Sitecore assemblies are restored through `PackageReference` from the two feeds in
[NuGet.config](NuGet.config): nuget.org and the public Sitecore feed
(`nuget.sitecore.com/resources`, no credentials needed). `Package.ps1` runs the restore for you.
To build in an IDE, open `Mediop.sln` as usual.

Sitecore assemblies are deliberately kept out of the build output (`ExcludeAssets=runtime`) — the
platform already ships them, and a second copy in the web root breaks assembly binding. `bin\Release\`
containing nothing but `Mediop.dll` is correct.

### If the build machine has no internet access

The first restore needs the network; after that packages come from the local NuGet cache. For a
genuinely air-gapped CI runner, mirror these six packages to an internal feed and repoint
`NuGet.config`:

| Package | Version | Source |
| --- | --- | --- |
| `Sitecore.Kernel` | 10.4.1 | public Sitecore feed |
| `Sitecore.Logging` | 10.4.1 | public Sitecore feed |
| `Sitecore.Mvc` | 10.4.1 | public Sitecore feed |
| `Sitecore.XA.Foundation.MediaRequestHandler` | 10.4.0 | public Sitecore feed |
| `Sitecore.XA.Foundation.Abstractions` | 10.4.0 | public Sitecore feed |
| `Microsoft.AspNet.Mvc` | 5.2.9 | nuget.org |

---

## Deploy

The short version: copy the package contents over each server's web root and recycle the app pool.
The full runbook — folder permissions, verification, rollback — is in [INSTALL.md](INSTALL.md).

```powershell
# Content management
Copy-Item "artifacts/Mediop-1.0.0/*" "C:/inetpub/wwwroot/cm.example.com/" -Recurse -Force

# Content delivery
Copy-Item "artifacts/Mediop-1.0.0/*" "C:/inetpub/wwwroot/cd.example.com/" -Recurse -Force
```

The same package goes to both roles. The difference in behaviour is handled by `role:require` inside
the config, not by shipping different packages. Identity Server does not serve media, so it needs
nothing.

After deploying, check `/sitecore/admin/showconfig.aspx` for the `mediopOptimize` pipeline, then open
an image on the site and look at `App_Data/logs/Mediop.log.*.txt`.

### Permissions required

The app pool identity must be able to:

- write to the temp folder (Windows temp, or whatever `Mediop.TempFilePath` points at)
- **execute** the `.exe` files in `App_Data/Mediop Tools`

If `App_Data` is mounted `noexec` or the folder is blocked by antivirus, optimization fails per image
and is logged — requests still succeed, serving the original bytes.

---

## Configuration

Everything lives in `App_Config/Include/Mediop/`. Files ending in `.disabled` or `.example` are
ignored by Sitecore; rename them to switch them on.

| File | Default | Purpose |
| --- | --- | --- |
| `Mediop.config` | on | Base pipeline, global settings, size window, off for the `shell` site |
| `Mediop.Roles.config` | on | Different tuning for CD and CM |
| `Mediop.Log.config` | on | Separate log at `Mediop.log.*.txt` |
| `Mediop.Jpeg.config` | on | JPEG via mozjpeg at quality 80; also lowers `Media.Resizing.Quality` from 95 to 80 |
| `Mediop.Jpeg.JpegOptim.config.disabled` | off | Swap the JPEG optimizer to jpegoptim instead of mozjpeg |
| `Mediop.Png.config` | on | PNG via pngquant then PngOptimizerCL |
| `Mediop.Gif.config` | on | GIF via gifsicle, lossless and animation-safe |
| `Mediop.Svg.config` | on | SVG via SVGO, and stops Sitecore resizing SVGs as bitmaps |
| `Mediop.Strategy.MediaCacheAsync.config` | on | Async strategy (the default) |
| `Mediop.Strategy.GetMediaStreamSync.config.disabled` | off | Sync strategy, for CDN scenarios |
| `Mediop.NextGenFormats.config` | on | WebP/AVIF media types + the format negotiation pipeline |
| `z.01.Mediop.NextGenFormats.WebP.config.disabled` | off | Enable WebP |
| `z.02.Mediop.NextGenFormats.Avif.config.disabled` | off | Enable AVIF |
| `z.03.Mediop.NextGenFormats.JpegXL.config.disabled` | off | Enable JPEG XL |
| `Mediop.NextGenFormats.CDN.config.disabled` | off | Per format cache key, for a CDN in front of CD |
| `Mediop.ExcludePaths.config.example` | off | Exclude specific media paths |
| `Mediop.TempFilePath.config.example` | off | Move the temp file folder |

### Settings

| Setting | Default | Notes |
| --- | --- | --- |
| `Mediop.TempFilePath` | empty | Temp file folder. Empty means the Windows temp folder |
| `Mediop.ToolTimeout` | 60000 | Per tool process timeout in ms (CD: 20000) |
| `Mediop.Async.MaxConcurrentThreads` | 1 | Background encoder threads (CD: 1, CM: 2) |
| `Mediop.Async.MaxQueueLength` | 500 | Pending optimizations before new ones are dropped (CM: 1000) |
| `Mediop.CDN.Enabled` | false | Turned on by the CDN config |

### Turning it off for one site

Add `enableMediop="false"` to the site definition. For an SXA site, fill in the field of the same
name on the Site Grouping item — no extra config needed.

---

## Strategy: CM versus CD

This is the part that decides whether optimization costs you anything at request time.

**Async (default, `Mediop.Strategy.MediaCacheAsync.config`).** Sitecore's media cache is replaced
with one that optimizes on a background thread. The first visitor to request a given image size gets
it resized but not yet optimized, and waits for nothing. Every request after that is served the
compressed bytes from cache.

This is what CD uses, with deliberate limits:

- **one** worker thread at `BelowNormal` priority, so the encoder only uses CPU that request handling
  is not using
- a queue capped at 500; when it is full, optimization for that request is skipped and the original
  image is still served — the next request tries again
- images under 2 KB and over 10 MB are skipped
- 20 second tool timeout instead of 60

The net effect: optimization load on CD has a hard ceiling and never enters the request path.

**Sync (`Mediop.Strategy.GetMediaStreamSync.config.disabled`).** Optimization happens inside
`getMediaStream`, so every response is compressed including the first — at the cost of making that
first visitor wait for the encoder. This only makes sense when a CDN pulls each image from the origin
exactly once. On a directly served site it makes the first visitor to each page pay for every image
on it.

Only one strategy may be active. Disable one before enabling the other, and read the SVG note in the
header of the sync config file.

---

## Next generation formats (WebP, AVIF, JPEG XL)

Off by default. Once enabled, images are transcoded according to the `Accept` header the browser
sent: a browser that supports WebP gets WebP, one that does not still gets JPEG. There is no
`<picture>` element or fallback logic to write in the front end.

To enable WebP:

1. Rename `z.01.Mediop.NextGenFormats.WebP.config.disabled` to `.config`.
2. Replace the media handler in `web.config`, because the stock one does not read `Accept`:

   ```xml
   <add verb="*" path="sitecore_media.ashx"
        type="Mediop.NextGenFormats.MediaRequestHandler, Mediop"
        name="Sitecore.MediaRequestHandler" />
   ```

   On an SXA site, check what is currently registered first — if it points at
   `Sitecore.XA.Foundation.MediaRequestHandler`, use `Mediop.NextGenFormats.MediaRequestHandlerXA,
   Mediop` instead so SXA's `mediaRequestHandler` pipeline keeps running. Getting this wrong will
   break SXA media features.

3. Recycle the app pool and verify:

   ```powershell
   curl.exe -H "Accept: image/webp,*/*" -I "https://cd.example.com/-/media/hero.ashx?w=800"
   ```

   After the second request (cache warm) `Content-Type` must be `image/webp`. Test without the header
   too — that must still return `image/jpeg`.

AVIF is added on top of WebP rather than instead of it: browsers that support AVIF list it first in
`Accept`, the rest still get WebP. AVIF encoding is far slower though, so on CD do not pair it with
the sync strategy.

**With a CDN in front of CD**, also enable `Mediop.NextGenFormats.CDN.config.disabled`. Without it the
CDN stores one entry per URL, so a browser with no WebP support can be served WebP from cache. That
config appends `extension=webp,avif` to media URLs and varies the rendering cache key to match.

---

## GIF

GIFs go through gifsicle, which is lossless and keeps animation intact. `Get-MediopTools.ps1` fetches
it from <https://eternallybored.org/misc/gifsicle/> — it ships as a zip rather than loose files, so
the script pulls `gifsicle.exe` and its license out of the archive.

That download is the one step that can fail without stopping the rest: if the site is unreachable or
the release layout changes, the script warns and carries on, and every other optimizer still works.
In that case either drop `gifsicle.exe` into `App_Data/Mediop Tools/gifsicle/` by hand, or delete
`Mediop.Gif.config` if the site serves no GIFs, so it does not log an error per GIF.

With WebP enabled, animated GIFs can also be converted to animated WebP through `gif2webp`.

---

## Verifying and troubleshooting

Logs are at `App_Data/logs/Mediop.log.<date>.txt`, one line per optimized image with before/after
sizes and duration. When investigating, change the level in `Mediop.Log.config` from `INFO` to
`DEBUG` — that also prints each tool's command line.

| Symptom | Most likely cause |
| --- | --- |
| Empty log, nothing optimized | No strategy active, or the site has `enableMediop="false"` |
| `could not be started` | Binary missing from `App_Data/Mediop Tools`, or blocked by antivirus |
| `exited with unexpected exit code` | Wrong tool arguments; run the command line from the DEBUG log by hand |
| `Error creating a temp file` | App pool cannot write to the temp folder |
| `optimization queue is full` | CD warming its cache; normal during a burst. If it persists, raise `Mediop.Async.MaxQueueLength` |
| Images unchanged although the log says optimized | Stale media cache. Clear `App_Data/MediaCache` and recycle |
| GDI errors for SVGs in the Sitecore log | `SvgIgnorer` is not running; make sure `Mediop.Svg.config` is active |

To start over: stop the app pool, empty `App_Data/MediaCache`, run again.

---

## Design notes

Decisions that were made on purpose, and why — useful when something needs to change later.

**The async queue is bounded, and the bound does its job.** `OptimizationQueue` uses a fixed capacity
`BlockingCollection` with its own worker threads rather than the thread pool. When the queue is full
the work is **dropped** and the original image is served — the next request tries again. That is
safer than an unbounded queue: a burst of cold media requests on CD cannot pile encoders up in memory
until the app pool falls over. A side effect is one less NuGet dependency.

**Worker threads run at `BelowNormal` priority.** Image encoders are CPU bound and share the machine
with request handling. Low priority keeps them on leftover CPU.

**Tuning is split by role, not by package.** `Mediop.Roles.config` uses `role:require`, so CD and CM
run the exact same DLL and the exact same config files with different numbers. One artifact for every
server, and no way to ship the wrong package to the wrong box.

**There is an upper and a lower size bound.** Files below the threshold are not worth the cost of
spawning a process; files above it can tie up the only worker for seconds. Both live in the
`SizeThreshold` processor.

**Exit codes that are not failures are not logged as errors.** pngquant returns 98/99 when a PNG
cannot be quantized profitably — a normal outcome for photographic content, not a failure. Treating
it as an error would fill the log with one ERROR per image and bury the real problems. The mechanism
is `IsSkipExitCode` on `CommandLineToolOptimizer` plus the `OptimizerArgs.Skipped` flag; other tools
can opt into it.

**Optimization must never corrupt an image.** `OptimizerProcessor` buffers the original bytes before
an optimizer runs. If the result fails, comes back larger, or is byte-for-byte the same size, the
original is used. There is no path by which a broken image reaches a browser.

**Next generation transcoding lives in one base class.** WebP, AVIF and JPEG XL share
`NextGenFormatOptimizer`: check browser support, set the output extension, then abort the pipeline —
because the optimizers after it do not understand the transcoded format. Adding a fourth format means
deriving from that class and filling in the tool arguments.

**Sitecore assemblies stay out of the output.** `ExcludeAssets=runtime` on every `PackageReference`.
The platform ships them already; a second copy in the web root breaks assembly binding.

**APIs deprecated in 10.4 have been replaced.** `MediaRequest.InnerRequestBase` supersedes
`InnerRequest`. The `MediaUrlOptions` overload is deliberately kept despite being marked obsolete,
because Sitecore itself still calls it.

**Anything that can be configuration is configuration.** `ToolTimeout` is a setting, not a constant.
`AdditionalToolArguments` accepts any value without validating its shape, so a tool with unusual
argument syntax can be plugged in without touching code.

**Optimizer binaries are never committed**, they are always downloaded by `Get-MediopTools.ps1`, on
every machine that builds. 57 MB of third party executables in git history is 57 MB that can never
be removed, and vendoring them means whoever clones the repo inherits binaries they did not see
being fetched. Running the script instead keeps the repository small, makes the provenance of every
binary explicit, and pulls each tool's license file alongside it — several are GPL and require
exactly that. A site that cannot reach the internet should mirror the tools internally and repoint
`$baseUrl`, not commit them here.

**JPEG resize quality defaults to 80.** `Mediop.Jpeg.config` lowers Sitecore's `Media.Resizing.Quality`
from 95, which is far past the point of visible benefit. This is the one setting Mediop changes
outside its own namespace, so it is worth knowing it applies to every Sitecore resize, not only the
images that pass through the optimizer. Deleting the `<settings>` block restores the platform
default.

**net48 only.** Sitecore 10.4 runs on .NET Framework 4.8; multi-targeting would add build surface
that nothing uses.
