# Third-party dependencies

The application's own source is licensed under [MIT](LICENSE). Dependencies retain their own licenses; the application's license does not relicense them.

This source repository does not contain prebuilt dependency DLLs or application packages. Restores obtain the original NuGet packages with their license information. A binary release must separately include the required notices/license texts and satisfy corresponding-source and replacement/relinking requirements for its shipped components.

## Application dependencies

| Package | Resolved version | License | Upstream |
|---|---|---|---|
| CompuMaster.Data | 3.0.2026.119 | MIT | https://github.com/CompuMasterGmbH/CompuMaster.Data |
| CompuMaster.Data.XlsEpplus | 3.0.2026.924 | MIT | https://github.com/CompuMasterGmbH/CompuMaster.Data.XlsEpplus |
| EPPlus | 4.5.3.3 | LGPL text supplied as lgpl-3.0.txt | https://www.nuget.org/packages/EPPlus/4.5.3.3 |
| Newtonsoft.Json | 13.0.4 | MIT | https://github.com/JamesNK/Newtonsoft.Json |
| System.Security.Cryptography.ProtectedData | 10.0.14 | MIT | https://github.com/dotnet/runtime |

EPPlus is intentionally constrained below version 5 and pinned to 4.5.3.3. The MIT XlsEpplus wrapper does not change EPPlus's LGPL obligations. The later EPPlus 5+ commercial/Polyform licensing model is not adopted. EPPlus 4.5.3.3 is a deprecated legacy release; this pin is a project compatibility/licensing decision, not an upstream support claim. Preserve the package's LGPL text and all applicable GPL/LGPL notice, corresponding-source and replacement/relinking requirements when distributing its binaries.

Other retained application dependencies include Microsoft.Extensions 10.0.14, modern Windows/runtime compatibility packages, System.Data.SqlClient 4.9.1 and native SQL SNI packages. Their exact versions, projects and license declarations are recorded in [the resolved license inventory](docs/dependency-license-inventory.json). Modern Microsoft packages use MIT; some older compatibility packages link the original .NET license information.

## Test-only dependencies

| Package | Version | License |
|---|---|---|
| NUnit | 5.0.0 | MIT |
| NUnit3TestAdapter | 6.3.1 | MIT |
| Microsoft.NET.Test.Sdk | 18.11.0 | MIT |
| NUnit.Analyzers | 4.15.0 | MIT |
| Microsoft.Testing.Platform components | 2.5.2 | MIT |
| Microsoft.TestPlatform.AdapterUtilities | 18.11.0 | MIT |
| Microsoft.ApplicationInsights | 2.23.0, test-platform dependency | MIT |

Test tooling is not an application dependency. Test-platform telemetry is disabled in CI. Application Insights remains on the consumer-compatible 2.x range pending [issue #7](https://github.com/CompuMasterGmbH/shopify-administration/issues/7); it is not an application telemetry feature.

CompuMaster.Data.Controls, GraphQL.Client/related packages, ShopifySharp, and their otherwise-unused transitive dependencies have been removed. No third-party source implementation from those packages is included here.

## Review and release obligations

- Review license/version changes whenever modifying Directory.Packages.props or lock files.
- See [the dependency review](docs/dependency-upgrade-review.md) for removals, compatibility decisions, deprecations and verification limits.
- Include package-specific copyrights/notices and required license texts in any future binary release; package references and this summary alone do not satisfy every redistribution obligation.
- Primary license references: [MIT](https://licenses.nuget.org/MIT), [LGPL-3.0](https://licenses.nuget.org/LGPL-3.0), and [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md).

## Avalonia desktop and native graphics

Avalonia 12.1.4 (including platform backends, Fluent theme and headless NUnit test tooling), DataGrid 12.1.2, MicroCom.Runtime and Tmds.DBus.Protocol declare MIT licenses. Avalonia Pro/XPF/commercial controls are not used. SkiaSharp 3.119.4 and HarfBuzzSharp 8.3.1.3 packages declare MIT; retain their native-component third-party notices as well. Avalonia.Angle.Windows.Natives supplies a BSD-style ANGLE LICENSE including copyright, redistribution conditions and disclaimer. Preserve that full file and native third-party notices in binary distributions. The exact restored versions/license declarations are recorded in the inventory; `tools/review-dependency-licenses.py` regenerates it from restored assets without recording machine paths.

Native graphics packages remain on the compatible ABI families selected by Avalonia rather than unverified SkiaSharp/HarfBuzz major overrides. Native runtime packages for unused platforms must be excluded or accounted for in platform release assets. No package in this migration introduces a required commercial licence; EPPlus retains its existing LGPL obligations.

## Packaged resources and source

Inter's font software retains its separate [SIL Open Font License](docs/licenses/Inter-OFL.txt), independently of the Avalonia wrapper's MIT package declaration. Self-contained distributions include the matching .NET runtime LICENSE and THIRD-PARTY-NOTICES as well as package-specific native notices. The resvg renderer is pinned build-time tooling and is not an application dependency or shipped executable.

Candidate packages include the exact original EPPlus 4.5.3.3 source archive pinned in tools/epplus-source.json, the applicable GPL/LGPL texts and [DLL replacement/relinking instructions](docs/lgpl-replacement.md), plus this application's source. See [Release assets](docs/release-assets.md) for the inventory, integrity checks and separate production-signing/publication gates.
