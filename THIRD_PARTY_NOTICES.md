# Third-party dependencies

The application's own source code is licensed under [MIT](LICENSE). Third-party libraries retain their own licenses; the application's license does not relicense those libraries.

This repository contains source code and NuGet package references, not prebuilt dependency DLLs or application releases. NuGet restores the original packages with their respective license information. A future binary release must separately include the required licenses, notices, and corresponding-source arrangements for the components it distributes.

## Direct references

| Package | Version | Declared license | Upstream |
|---|---|---|---|
| CompuMaster.Data | 3.0.2022.311 | MIT | https://github.com/CompuMasterGmbH/CompuMaster.Data |
| CompuMaster.Data.Controls | 3.0.2022.311 | MIT | https://github.com/CompuMasterGmbH/CompuMaster.Data |
| CompuMaster.Data.XlsEpplus | 3.0.2021.715 | MIT | https://github.com/CompuMasterGmbH/CompuMaster.Data.XlsEpplus |
| GraphQL.Client | 4.0.2 | MIT | https://github.com/graphql-dotnet/graphql-client |
| Newtonsoft.Json | 13.0.1 | MIT | https://github.com/JamesNK/Newtonsoft.Json |
| ShopifySharp | 5.15.0 | MIT | https://github.com/nozzlegear/ShopifySharp |
| System.Data.SqlClient | 4.8.6 | MIT | https://github.com/dotnet/SqlClient |
| System.Security.Cryptography.ProtectedData | 6.0.0 | MIT | https://github.com/dotnet/runtime |

Versions above correspond to the project references at the initial public import. Review the resolved dependency graph again when updating packages.

## Material transitive dependencies

- **EPPlus 4.5.3.2** is restored through CompuMaster.Data.XlsEpplus and is licensed under **LGPL-3.0-or-later**. It is not covered by the MIT license of the wrapper or this application. The later EPPlus 5+ commercial/Polyform licensing model does not apply to version 4.5.3.2. See the [exact package](https://www.nuget.org/packages/EPPlus/4.5.3.2), [upstream source](https://github.com/JanKallman/EPPlus), and [LGPL terms](https://licenses.nuget.org/LGPL-3.0-or-later). Distributing EPPlus binaries requires satisfying the applicable GPL/LGPL notice, corresponding-source and replacement/relinking conditions. Keep this check part of any future binary-release process.
- GraphQL.Client.Abstractions, GraphQL.Client.Abstractions.Websocket and GraphQL.Primitives 4.0.2 use the MIT license included as LICENSE.txt in the packages.
- System.Reactive 4.3.2 uses Apache-2.0.
- Microsoft.Extensions 2.2.0 packages refer to the ASP.NET [Apache-2.0 license](https://raw.githubusercontent.com/aspnet/AspNetCore/2.0.0/LICENSE.txt).
- Other Microsoft/System runtime packages use their package-specific MIT or earlier [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md), including any third-party notices bundled with those packages.

Primary license texts: [MIT](https://licenses.nuget.org/MIT), [Apache-2.0](https://licenses.nuget.org/Apache-2.0), [LGPL-3.0-or-later](https://licenses.nuget.org/LGPL-3.0-or-later). Package license declarations can be inspected in the exact-version .nuspec files supplied by NuGet.
