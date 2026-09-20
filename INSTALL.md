# Mediop — Installing on CM and CD

Deployment runbook for the image optimization module. `{CM-ROOT}`, `{CD-ROOT}` and `{domain}` are
placeholders — replace them with the actual web root paths and app pool names in your environment.

The same package goes to both roles. The difference in behaviour is handled by `role:require` inside
the config, not by shipping different packages. Identity Server does not serve media, so it does
**not** need this.

---

## Contents

1. [Prerequisites](#1-prerequisites)
2. [Build the artifact](#2-build-the-artifact)
3. [Deploy to CM](#3-deploy-to-cm)
4. [Deploy to CD](#4-deploy-to-cd)
5. [Verify](#5-verify)
6. [Optional: enable WebP](#6-optional-enable-webp)
7. [Rollback](#7-rollback)
8. [Checklist](#8-checklist)

---

## 1. Prerequisites

| Requirement | Notes |
|-------------|-------|
| MSBuild | 2022 (v17+), on the build machine only |
| Network access on the build machine | Once, for the first NuGet restore (see README if air-gapped) |
| Access to `{CM-ROOT}` and `{CD-ROOT}` | Read/Write |
| App pool identity | Can write to the Windows temp folder |
| App pool identity | Can **execute** `.exe` files under `App_Data\Mediop Tools` |
| Antivirus | `App_Data\Mediop Tools` is not blocked |

Those last two are the most common cause of a failed install. The module works by invoking native
binaries as separate processes; if execution is blocked, every image fails to optimize (logged, and
the request still succeeds with the original bytes).

### Confirm the role is defined

`Mediop.Roles.config` uses `role:require`, which depends on `role:define` in each server's web.config.
Check it first — if CD is not declared as `ContentDelivery`, the CD specific tuning will not apply
and CD will fall back to the defaults.

```powershell
Select-String -Path "{CD-ROOT}\web.config" -Pattern "role:define"
```

You should see `role:define="ContentDelivery"`. On CM: `ContentManagement` or `Standalone`.

---

## 2. Build the artifact

Run this on the build machine.

```powershell
cd <sitecore-mediop folder>

# once per machine; downloads the optimizer binaries (57 MB)
.\build\Get-MediopTools.ps1

.\build\Package.ps1 -Version 1.0.0
```

The result in `artifacts\Mediop-1.0.0\` already mirrors the web root:

```
artifacts\Mediop-1.0.0\
├── bin\Mediop.dll
├── App_Config\Include\Mediop\          ← 17 .config files
└── App_Data\Mediop Tools\              ← 23 binaries + their licenses
```

Copy that folder to your staging location, or straight to the server.

---

## 3. Deploy to CM

### Step 1 — Stop the app pool

```powershell
Stop-WebAppPool -Name "cm.{domain}"
```

### Step 2 — Copy the DLL

```powershell
Copy-Item "artifacts\Mediop-1.0.0\bin\Mediop.dll" "{CM-ROOT}\bin\" -Force
```

One DLL. No third party dependencies to bring along, and **no binding redirects to change** — the
module only uses assemblies the platform already has.

### Step 3 — Copy the config

```powershell
New-Item "{CM-ROOT}\App_Config\Include\Mediop" -ItemType Directory -Force | Out-Null
Copy-Item "artifacts\Mediop-1.0.0\App_Config\Include\Mediop\*" `
    "{CM-ROOT}\App_Config\Include\Mediop\" -Force
```

Active after the copy (`.disabled` and `.example` files are ignored by Sitecore):

| File | Purpose |
|------|---------|
| `Mediop.config` | `mediopOptimize` pipeline, global settings, size window, off for the `shell` site |
| `Mediop.Roles.config` | Different tuning for CD and CM |
| `Mediop.Log.config` | Separate `Mediop.log.*.txt` |
| `Mediop.Jpeg.config` | JPEG via mozjpeg + lowers `Media.Resizing.Quality` 95 → 80 |
| `Mediop.Png.config` | PNG via pngquant then PngOptimizerCL |
| `Mediop.Gif.config` | GIF via gifsicle |
| `Mediop.Svg.config` | SVG via SVGO + stops Sitecore resizing SVGs as bitmaps |
| `Mediop.Strategy.MediaCacheAsync.config` | Async strategy |
| `Mediop.NextGenFormats.config` | WebP/AVIF media types (transcoding still off) |

> **About `Mediop.Jpeg.config`:** it sets the global `Media.Resizing.Quality` to **80**, down from
> the Sitecore default of 95. That is the intended default here — 95 is far past the point of visible
> benefit and only costs bandwidth. Be aware of the scope though: this is a global setting, so it
> applies to **every** Sitecore resize, not only the images Mediop optimizes. To keep the platform
> default instead, delete the `<settings>` block from that file.

### Step 4 — Copy the optimizer binaries

```powershell
New-Item "{CM-ROOT}\App_Data\Mediop Tools" -ItemType Directory -Force | Out-Null
Copy-Item "artifacts\Mediop-1.0.0\App_Data\Mediop Tools\*" `
    "{CM-ROOT}\App_Data\Mediop Tools\" -Recurse -Force
```

Do not separate the license files from the binaries — several of the tools are GPL and require their
license to be distributed with them.

### Step 5 — Grant execute permission to the app pool

```powershell
$acct = "IIS AppPool\cm.{domain}"
$path = "{CM-ROOT}\App_Data\Mediop Tools"

$acl  = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $acct, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $path $acl
```

If the app pool runs under a domain service account, use that account instead of `$acct`.

### Step 6 — Start the app pool

```powershell
Start-WebAppPool -Name "cm.{domain}"
```

No `web.config` change is needed for a basic install. The handler change only applies if you enable
WebP/AVIF — see [section 6](#6-optional-enable-webp).

---

## 4. Deploy to CD

Identical steps, different root and app pool name. No file differs in content.

```powershell
Stop-WebAppPool -Name "cd.{domain}"

# DLL
Copy-Item "artifacts\Mediop-1.0.0\bin\Mediop.dll" "{CD-ROOT}\bin\" -Force

# Config
New-Item "{CD-ROOT}\App_Config\Include\Mediop" -ItemType Directory -Force | Out-Null
Copy-Item "artifacts\Mediop-1.0.0\App_Config\Include\Mediop\*" `
    "{CD-ROOT}\App_Config\Include\Mediop\" -Force

# Optimizer binaries
New-Item "{CD-ROOT}\App_Data\Mediop Tools" -ItemType Directory -Force | Out-Null
Copy-Item "artifacts\Mediop-1.0.0\App_Data\Mediop Tools\*" `
    "{CD-ROOT}\App_Data\Mediop Tools\" -Recurse -Force

# Execute permission
$acct = "IIS AppPool\cd.{domain}"
$path = "{CD-ROOT}\App_Data\Mediop Tools"
$acl  = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $acct, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $path $acl

Start-WebAppPool -Name "cd.{domain}"
```

What `role:require="ContentDelivery"` applies automatically:

| Setting | CD value | Why |
|---------|----------|-----|
| `Mediop.Async.MaxConcurrentThreads` | 1 | Encoders are CPU bound and share the box with request handling |
| `Mediop.Async.MaxQueueLength` | 500 | Queue full → skip optimization, still serve the original |
| `Mediop.ToolTimeout` | 20000 ms | Fail fast rather than holding a worker for a minute |
| Size window | 2 KB – 10 MB | Outside that range it is not worth the CPU |

Worker threads run at `BelowNormal` priority, so optimization only consumes CPU that request handling
is not using. The async strategy means no visitor ever waits for an encoder: the first to request a
given image size gets the unoptimized version, everyone after that is served from cache.

---

## 5. Verify

### 5a. Config is being read

Open `/sitecore/admin/showconfig.aspx` and search for `mediopOptimize`. It should appear with the
`DisableMediopForSite` and `SizeThreshold` processors, then one `ExtensionBasedOptimizer` per format.

On CD, also check `Mediop.Async.MaxQueueLength` — a value of 500 means `role:require` is working. If
it does not appear at all, `Mediop.config` is not being read.

### 5b. Optimization is running

Open an image from the media library in a browser, **refresh once** (the first request is genuinely
unoptimized because the strategy is async), then:

```powershell
Get-Content "{CD-ROOT}\App_Data\logs\Mediop.log.$(Get-Date -Format yyyyMMdd).txt" -Tail 20
```

Expected:

```
Mediop: optimized /Images/hero.jpg [requested: 800w 145200 bytes] [final: 98431 bytes]
[saved 46769 bytes / 32.21%] [took 214ms] [extension jpg]
```

### 5c. The response really did shrink

```powershell
$url = "https://cd.{domain}/-/media/Images/hero.ashx?w=800"
1..2 | ForEach-Object {
    (Invoke-WebRequest $url -UseBasicParsing).RawContentLength
}
```

The second number must be smaller than the first.

### 5d. If the log is empty

In order:

1. Was the app pool restarted after the config was copied?
2. Is `Mediop.dll` in `{ROOT}\bin`?
3. Is the test image above 2 KB (CD) or 1 KB (CM)?
4. Does the site being hit have `enableMediop="false"`?
5. Set the level in `Mediop.Log.config` from `INFO` to `DEBUG`, restart, retry — DEBUG prints each
   tool's command line, which makes an execution failure obvious.

---

## 6. Optional: enable WebP

Do this **after** the basic install is proven to work, not at the same time — otherwise a problem is
hard to attribute.

### Step 1 — Enable the config

```powershell
Rename-Item "{CD-ROOT}\App_Config\Include\Mediop\z.01.Mediop.NextGenFormats.WebP.config.disabled" `
    "z.01.Mediop.NextGenFormats.WebP.config"
```

### Step 2 — Replace the media handler in web.config

The stock handler does not read the `Accept` header, so it has to be swapped. In
`<system.webServer><handlers>`:

```xml
<add verb="*" path="sitecore_media.ashx"
     type="Mediop.NextGenFormats.MediaRequestHandler, Mediop"
     name="Sitecore.MediaRequestHandler" />
```

> **On an SXA site,** check what is registered first. If its `type` points at
> `Sitecore.XA.Foundation.MediaRequestHandler`, use `Mediop.NextGenFormats.MediaRequestHandlerXA,
> Mediop` instead so SXA's `mediaRequestHandler` pipeline keeps running. Choosing wrong here breaks
> SXA media features.

### Step 3 — Restart and test

```powershell
Restart-WebAppPool -Name "cd.{domain}"

curl.exe -H "Accept: image/webp,*/*" -I "https://cd.{domain}/-/media/Images/hero.ashx?w=800"
```

After the second request, `Content-Type` must be `image/webp`. Test without that header too — it must
still return `image/jpeg`. If both return the same thing, the handler was not replaced.

### With a CDN in front of CD

You must also enable `Mediop.NextGenFormats.CDN.config.disabled`. Without it the CDN stores one entry
per URL, so a browser with no WebP support can be served WebP from cache and fail to render it.

---

## 7. Rollback

Fast and non-destructive — the module touches no database and no items.

```powershell
Stop-WebAppPool -Name "cd.{domain}"

Remove-Item "{CD-ROOT}\App_Config\Include\Mediop" -Recurse -Force
Remove-Item "{CD-ROOT}\bin\Mediop.dll" -Force
Remove-Item "{CD-ROOT}\App_Data\Mediop Tools" -Recurse -Force

# optimized images already written to cache
Remove-Item "{CD-ROOT}\App_Data\MediaCache\*" -Recurse -Force

Start-WebAppPool -Name "cd.{domain}"
```

If you replaced the web.config handler (section 6), restore its original `type` **before** removing
the DLL — otherwise every media request fails on a missing handler type.

To disable temporarily without removing anything, rename `Mediop.config` to `Mediop.config.disabled`
and recycle the app pool.

---

## 8. Checklist

Per server, CM and CD each:

- [ ] `role:define` in web.config is correct
- [ ] App pool stopped
- [ ] `Mediop.dll` → `{ROOT}\bin\`
- [ ] 17 config files → `{ROOT}\App_Config\Include\Mediop\`
- [ ] Binaries + licenses → `{ROOT}\App_Data\Mediop Tools\`
- [ ] ReadAndExecute granted to the app pool identity on the tools folder
- [ ] App pool started
- [ ] `mediopOptimize` appears in showconfig
- [ ] An `optimized` line appears in `Mediop.log.*.txt` after the second refresh
- [ ] Response size actually dropped

Once that has been stable for a few days, consider WebP (section 6).
