# Notice

Mediop itself is licensed under MIT; see [LICENSE](LICENSE).

It is an implementation of the architecture of [Dianoga](https://github.com/kamsar/Dianoga) by
Kam Figy, also published under the MIT license. The pipeline design, the configuration layout and
the strategy split come from that project. The code here was written against Sitecore 10.4.1 and
departs from it in several places, which the README documents under "Design notes".

The optimizer executables that `build/Get-MediopTools.ps1` downloads are third party builds, each
under its own license. Those licenses are downloaded alongside the binaries and ship inside
`App_Data/Mediop Tools`. Keep them there.

| Tool | Project | License |
| --- | --- | --- |
| cjpeg, jpegtran | [mozjpeg](https://github.com/mozilla/mozjpeg) | BSD-3-Clause / IJG |
| pngquant | [pngquant](https://pngquant.org/) | GPL-3.0 (dual licensed, commercial available) |
| PngOptimizerCL | [PNGOptimizer](https://psydk.org/pngoptimizer) | GPL-2.0 |
| svgo-win | [SVGO](https://github.com/svg/svgo) via [svgo-executable](https://github.com/Antonytm/svgo-executable) | MIT |
| cwebp, gif2webp | [libwebp](https://chromium.googlesource.com/webm/libwebp) | BSD-3-Clause |
| avifenc | [libavif](https://github.com/AOMediaCodec/libavif) | BSD-2-Clause |
| cjxl | [libjxl](https://github.com/libjxl/libjxl) | BSD-3-Clause |
| jpegoptim | [jpegoptim](https://github.com/tjko/jpegoptim) | GPL-3.0 |
| gifsicle (optional, not bundled) | [gifsicle](https://www.lcdf.org/gifsicle/) | GPL-2.0 |

The GPL licensed tools are invoked as separate processes, not linked into the assembly.
