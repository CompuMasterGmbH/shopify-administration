# Installation and update policy

## Current qualification and downloads

The primary frontend is Avalonia/.NET 10. Qualified CI candidates exist for Windows x64, Linux x64/X11 and macOS ARM64. They are self-contained portable packages, with retained licences, exact EPPlus 4.5.3.3 source and replacement instructions. Candidate artifacts are unsigned/ad-hoc-signed test builds, not trusted stable installers. Native CI startup is distinct from clean-machine publisher-trust and real signed N-to-N+1 qualification, tracked in #14.

No production application release is published by implementing this feature. Source/development/candidate builds have no enabled stable updater. Only an installation with a matching version/RID marker, approved channel and completed qualification can use stable/preview update metadata. Package selection and all known limitations are documented in [Release assets](release-assets.md).

## Windows ClickOnce and portable installs

ClickOnce is a Windows technology. The pinned official Microsoft.DotNet.Mage 10.0.0 tool generates the modern .NET launcher, application manifest and deployment manifest. The launcher is managed/MSIL; the package metadata, native payload and update selector are explicitly Windows x64. Do not advertise this manifest as an ARM64 or cross-platform package. The licence materials are retained in third-party-licenses.zip beside the application, avoiding ClickOnce path-length limits; source/replacement instructions remain included.

`tools/build-clickonce.ps1` stages a new version directory and has no installation/trust side effects. Candidate mode uses an isolated, unsigned channel. Stable/preview generation requires a publisher certificate and verifies signed manifests. `tools/verify-clickonce.py` checks the complete file/hash/reference chain. CI builds two increasing candidate manifest versions, checks stable application identity and rejects an intentionally corrupt new payload. This is manifest/staging qualification, not a claim that an unsigned candidate was installed or a real signed N-to-N+1 upgrade passed.

For a qualified released installer, open the HTTPS channel .application link through Windows' normal ClickOnce handler and publisher-trust flow. The application checks metadata asynchronously after startup; it does not block startup on an additional native beforeApplicationStartup subscription. Opening a newly offered ClickOnce manifest lets the OS handle installation, identity/signature/integrity verification and version activation. Modern .NET exposes ClickOnce deployment properties through ClickOnce_* environment variables, not ApplicationDeployment update methods. No failed update should disable the currently installed version through a forced minimum version policy.

Portable ZIP releases remain a supported alternative, including for LGPL library replacement or self-rebuilding. Unpack a complete package into a new version directory, retain licences/source and start ShopifyAdministration.Desktop.exe. Preserve the existing user profile/settings outside the installation. Coordinate replacement of running applications; never replace an unrelated active instance or delete an operational profile automatically.

## Linux and macOS per-user installation

Linux packages require X11, fontconfig, libice and libsm as described in [the platform matrix](release-assets.md). macOS packages contain an .app bundle. The normal production macOS path requires Developer ID signing/notarization and Gatekeeper acceptance; never disable trust/security barriers for a candidate.

After verifying the downloaded archive checksum and retaining all licence/source material, the optional install-user.py helper installs a complete Linux/macOS package without administrator privileges. It verifies the file inventory, refuses other architectures or an unrelated existing launcher, stages a new version directory and atomically switches an owned launcher/symlink. Prior versions stay intact for rollback. Linux gets a per-user desktop entry and icon; macOS uses ~/Applications with complete bundle metadata. macOS copying uses ditto to preserve signatures/resources. Qualified production macOS installs also run normal signature/Gatekeeper assessment.

Use `python3 install-user.py` from the extracted package. The helper refuses unqualified candidates unless the explicit --allow-candidate test option is supplied. CI uses a unique test-owned --home directory and performs no operation on an operator's real home/profile. It tests initial staging, version-directory/launcher switching, retention of a sentinel profile and rejection of a corrupt update; real signed binary N-to-N+1 installation qualification remains #14. No previous version or user data is recursively deleted by installation. Uninstall the chosen application/launcher manually or through its package manager; encrypted profiles/preferences are not silently purged.

## Update behavior and trust

The app checks once automatically per process session and provides Nach Updates suchen for explicit repeated checks. Checks have a bounded timeout, leave login/data I/O responsive, and use a separate HTTP client with no Shopify authorization, cookies or default credentials. Automatic offline/metadata failures are quiet; manual checks explain the failure. Später dismisses for the session; Version überspringen persists a choice scoped to app/channel/RID/deployment. Manual checks ignore a skip.

The selector validates app identity, schema, approved release status, deterministic numeric/prerelease ordering and exact OS/architecture/channel/deployment. Stable feeds cannot offer prereleases. It never offers a downgrade/equivalent version, an unapproved target, another OS's asset or an arbitrary host/URL. Only the repository's HTTPS release-download path and exact channel ClickOnce manifest are launchable; redirects, credentials, query/fragment injection and encoded path traversal are rejected for metadata/destinations. Downloads open through a platform handler rather than executing a downloaded binary inside the app. The advertised SHA-256 accompanies the download; opening a browser alone does not verify downloaded bytes. ClickOnce and the per-user installer perform their respective manifest/inventory checks; publisher signing/notarization is a separate production gate.

Version parsing supports v prefixes, two-to-four numeric components, ignored build metadata and SemVer-style prerelease ordering, without lexical fallback. A package-managed installation delegates to its package manager and never runs a competing updater. Package integration must mark deploymentType=package-manager; known WindowsApps/Homebrew locations are also recognized. No .deb/.rpm/Store distribution is claimed by portable qualification alone. Unknown/development installations explain that no qualified channel is enabled.

The normal feed is https://compumastergmbh.github.io/shopify-administration/app-updates.json. Repository Pages activation/publication is a separate approved deployment action; the local artifact builder can be reviewed without publishing. The initial feed is empty. Release feed generation checks that the exact published version/source revision and every referenced asset/checksum exist before preparing a new local snapshot. Existing ClickOnce payloads and feed state are retained. Publication must not activate metadata for an incomplete release.

## Profiles and production gates

Encrypted Windows credentials remain in the existing stable per-user application data location; they never move into a version-specific ClickOnce/App bundle directory. Linux/macOS retain the explicit safe session-only credential fallback. Update preferences contain only skipped version keys, live in a separate non-secret per-user file and are written atomically. Cancellation, malformed/locked settings and upgrade failure must not reset a credential profile.

Production preparation requires a reviewed, version/channel/RID-specific qualification record; the example in docs/releases/qualification.example.json is intentionally unapproved. No signing credentials are provided or created here. Configure publisher/notarization material only in the approved protected CI context. Certificate/private-key data must never enter source, logs, packages or site state. Actual signed installer/trust/DPI/accessibility/real-upgrade qualification is recorded in #14, and public application release authorization remains separate.

## Primary documentation and reference

- [Microsoft ClickOnce for modern .NET](https://learn.microsoft.com/en-us/visualstudio/deployment/clickonce-deployment-dotnet)
- [ClickOnce environment properties](https://learn.microsoft.com/en-us/visualstudio/deployment/access-clickonce-deployment-properties-dotnet)
- [Official dotnet-mage launcher and manifest workflow](https://github.com/dotnet/deployment-tools/blob/main/docs/dotnet-mage/README.md)
- [GuacamoleClient installation/update reference](https://github.com/jochenwezel/GuacamoleClient/blob/main/docs/clickonce/INSTALL.md)

The reference informed separation of channel/deployment identity, once-per-session checks and skipped-version preferences. Project identifiers, URLs, certificates and update policy are this application's own; no Guacamole-specific endpoint or key was copied.
