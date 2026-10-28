# Release assets and supported targets

## Qualified candidate matrix

| OS / architecture | Frontend / runtime | Package | Native dependencies / storage | Status |
| --- | --- | --- | --- | --- |
| Windows x64 | Avalonia, self-contained .NET 10.0.14 | ZIP | Windows 10 22H2/11 within .NET/Avalonia support; DPAPI CurrentUser | CI native unpacked startup; ClickOnce inputs in #4 |
| Linux x64 / glibc / X11 | Avalonia, self-contained .NET 10.0.14 | tar.gz, PNG, desktop-entry input | X11, libice, libsm and fontconfig; session-only profile fallback | Ubuntu CI/Xvfb native unpacked startup |
| macOS ARM64 | Avalonia, self-contained .NET 10.0.14 | ZIP containing .app and supporting licence/source material | macOS 14+ within .NET/Avalonia support; session-only profile fallback | macOS CI native bundle startup; local ad-hoc signing only |
| Windows ARM64, Linux ARM64, macOS x64 | Candidate future RIDs | None advertised | Requires separate native package/startup and installation qualification | Evaluated as potential Avalonia targets; not released or claimed supported here |

The primary application packages cover the three qualified desktop targets. The WinForms compatibility frontend is built/tested but is not separately advertised as the future desktop distribution. Mobile, browser, musl/Alpine, headless server sessions and untested architecture combinations are not desktop release targets. See [Avalonia platform requirements](https://docs.avaloniaui.net/docs/supported-platforms) and [.NET supported OS policy](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md).

## Reproducible inputs and resources

The project-owned MIT vector source is `assets/app-icon.svg`, an application-specific cart/chart symbol, not Shopify's trademarked logo. The legacy publisher icon remains in the compatibility frontend. The pinned build-time MIT resvg 2.6.2 tool and npm lock file generate PNG at 16/32/48/64/128/256/512/1024 pixels, PNG-compressed multiresolution ICO, modern ICNS representations and embedded desktop window/executable icons. No system fonts, commercial controls or image-generation service are required. `npm ci --ignore-scripts` followed by `npm run generate` / `npm run verify` in tools/assets reproduces/compares every byte against committed SHA-256 values on each CI platform. New graphics are editable in the common SVG source.

The .NET SDK baseline is 10.0.403 (latest-patch roll-forward allowed locally); candidate packaging requests that SDK and pins the runtime to 10.0.14. Central package versions and all supported runtime graphs are locked. Publishing is self-contained, untrimmed, non-AOT and not a single file, retaining replaceable LGPL assemblies. Native resources/RID-specific dependencies come from the matching reviewed runtime graph. Source and generated resource changes must be committed together.

## Candidate CI pipeline

`Package candidate assets` runs on pull requests, main pushes and manual dispatch with read-only repository permission. It verifies deterministic icons, publishes each RID, rejects missing dependencies, test tooling, credential-like filenames/contents and private key material, and stages a new package directory rather than replacing an installation. It includes:

- Application executable, managed/native dependencies and the matching self-contained runtime.
- Product/version/RID/frontend/channel/commit metadata with stableApproved=false; candidates are never described as signed stable releases.
- Platform icon/desktop/bundle metadata, native window/executable icons and macOS high-DPI Info.plist.
- Application MIT licence, third-party notices, exact NuGet licence/native notice files, .NET runtime notices and the Inter font's separate SIL Open Font License.
- EPPlus LGPL/GPL texts, pinned corresponding source archive, original build recipe, application source archive and explicit replacement/relinking instructions.
- A complete per-file SHA-256 inventory, portable archive and archive SHA-256 checksum.

Each package's actual executable is launched from its staged location in no-network/no-operational-profile startup mode, then the file inventory is reverified before archiving/uploading the candidate. macOS bundles receive an ad-hoc signature and verification for local testing; this is not Developer ID signing or notarization. Windows test packages are unsigned. ZIP/tar archives are version/RID-labelled and preserve Unix executable permissions. Hashes detect accidental corruption; they do not replace publisher signatures.

## Licensing and replacement

EPPlus stays exactly on 4.5.3.3, never 5+. `tools/epplus-source.json` pins the original Version 4.5.3.3 commit and archive hash; packaging checks its declared assembly/package version. The package contains that complete source and [replacement instructions](lgpl-replacement.md), not only a moving download link. All dependencies keep their separate rights; native Skia/ANGLE/Inter notices must accompany their binaries. If correspondence/licensing checks fail, packaging fails before artifact upload.

## Download and publication policy

Select an archive matching OS and CPU architecture; verify its SHA-256 against the accompanying checksum, unpack it, and retain the licence/source directories. Linux additionally needs the listed OS libraries. macOS .app and Windows ZIP candidates are developer test packages, not trusted clean-machine installers; platform trust barriers must not be disabled to install them.

CI artifacts are short-lived qualification outputs, not a production release or update channel. Stable publication requires separate user release authorization, successful tests for the exact revision, verified package contents, publisher signing/notarization where applicable, and installation/update qualification under #4. Signing keys belong in protected CI secrets; never commit them. A future release workflow must publish all referenced packages/source/checksums before activating update metadata, and must withhold unsupported or unsigned targets from a trusted stable feed. No production release is created by this workflow.
