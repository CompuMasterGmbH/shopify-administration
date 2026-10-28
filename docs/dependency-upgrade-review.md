# Dependency upgrade and NUnit migration review

Review date: 2026-10-28. Scope: issues #5 and #6.

## Package decisions

| Dependency | Previous version | Current decision |
|---|---|---|
| CompuMaster.Data | 3.0.2022.311 | Upgrade to 3.0.2026.119; retain nullable-to-DBNull helpers used by data mapping. |
| CompuMaster.Data.Controls | 3.0.2022.311 | Remove: the frontend uses standard WinForms controls, with no usages of this package. |
| CompuMaster.Data.XlsEpplus | 3.0.2021.715 | Upgrade to 3.0.2026.924; validate actual workbook writing and reading. |
| EPPlus | 4.5.3.2 | Upgrade only within 4.x to 4.5.3.3, explicitly pinned to that exact version. Never adopt 5 or later. |
| Newtonsoft.Json | 13.0.1 | Upgrade to 13.0.4 in all consuming projects through central versions. |
| System.Security.Cryptography.ProtectedData | 6.0.0 | Upgrade to 10.0.14; retain OS guards and DPAPI CurrentUser file format. |
| System.Data.SqlClient | 4.8.6 | Upgrade inherited compatibility dependency to 4.9.1. See the limitation below. |
| GraphQL.Client and related packages | 4.0.2 | Remove unused unfinished GraphQL adapter and packages; it had no callers or working initialized client. |
| ShopifySharp | 5.15.0 | Remove unused adapter and package. The real UI continues to use the shared REST transport. |
| NUnit / NUnit3TestAdapter | Not used | Introduce 5.0.0 / 6.3.1. |
| Microsoft.NET.Test.Sdk / NUnit.Analyzers | Not used | Introduce 18.11.0 / 4.15.0. |
| Microsoft.Testing.Platform stack | Adapter default 2.4.1 | Pin matching components to stable 2.5.2; test discovery/execution verified. |
| Runtime and framework compatibility dependencies | Various older versions | Central compatible pins, including Microsoft.Extensions 10.0.14, ODBC/OLE DB/DirectoryServices/Drawing 10.0.14, XML XPath 4.7.0, native SQL SNI 4.7.0, and NETCore.Platforms 7.0.4. |

The removed application adapters were not shared-library contracts and had no active retrieval callers. This removes dead placeholders, not delivered GraphQL/ShopifySharp functionality. New API work belongs to #2 and must choose supported Shopify operations explicitly.

## Intentional exceptions and limitations

- EPPlus 4.5.3.3 remains below 5 by explicit project instruction. The central exact-version pin and resolved-assembly regression test prevent a silent major upgrade. This package is a legacy/deprecated line; retaining it is not a claim of ongoing upstream support.
- Microsoft.ApplicationInsights 2.23.0 remains test-only through Microsoft.Testing.Extensions.Telemetry 2.5.2. A central range excludes 3.x pending the explicit compatibility work in #7. [Microsoft documents architectural and API breaking changes in 3.x](https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/BreakingChanges.md). No 3.x runtime failure is claimed to have been reproduced here. Tests and CI disable test-platform telemetry; the application does not reference this telemetry stack.
- System.Data.SqlClient 4.9.1 is the latest legacy-package version found during review and is inherited through CompuMaster.Data. Its package documentation marks it deprecated. Switching to Microsoft.Data.SqlClient requires an upstream dependency/API decision; replacing its identity underneath the existing library is not a package-version update. The application currently uses CompuMaster.Data's nullable mapping helpers, not database access; #8 tracks removing/replacing this inherited dependency.
- The shared core still targets netstandard2.0. Platform and application projects remain .NET 10; no framework migration is hidden in this upgrade.
- All selected direct packages and the resolved transitive graph were reviewed using exact-version NuGet metadata. See [the inventory](dependency-license-inventory.json). The graph includes MIT, the package-supplied LGPL text, and older .NET MIT-license links; no commercial-only package was selected.

## Reproducibility and security review

Versions are centralized in Directory.Packages.props. Intentional compatible transitive pins avoid old runtime/test dependencies. Each project commits packages.lock.json; CI restores in locked mode. NuGet audit covers direct and transitive dependencies, and downgrade/vulnerability warnings fail restore/build.

The local vulnerability query reported no known vulnerable packages in any of the five projects at review time. This is advisory-database evidence, not an exhaustive source/security audit. The only remaining newer-version results were the deliberate EPPlus boundary and test-only Application Insights 3.x follow-up.

## Verification

- Windows Release solution build: zero warnings and errors.
- NUnit Core/session/HTTP/DPAPI tests: 49 passed.
- NUnit Windows UI/data-adapter/export tests: 15 passed, using fake credentials/HTTP and test-owned files.
- Workbook export test writes and reads back Unicode text, integer and decimal values through the updated XlsEpplus/EPPlus packages.
- Controlled API tests cover login/authentication, recipient restrictions and multi-page retrieval; no real Shopify request is needed.
- A new damaged-profile test exposed InvalidDataException escaping the store's safe error mapping. The exception filter now handles it, and the regression passes without modifying a real credential profile.
- CI runs shared tests on Windows/Linux/macOS and Windows UI/export tests, publishing TRX results. CI outcomes are recorded separately in the implementing PR.

No production release or live shop-data mutation is part of this work.
