# Shopify-Administration

A Shopify administration desktop application. The primary frontend is Avalonia 12 on .NET 10, with Windows/Linux/macOS qualification described in [the framework decision](docs/ui-framework-decision.md).

## Login and local storage

- Enter the complete HTTPS API base URL, for example `https://shop.myshopify.com/admin/api/2023-01/`. Both shop and API version are configurable; no operational shop is embedded in the application.
- For existing Basic authentication, enter the API key and password/app token. For token-only authentication, leave the API-key field empty.
- Login is validated through a read-only shop-information request. Invalid credentials, network errors, or denied permissions do not activate a session.
- **Remembering credentials defaults to off**, including prefilled dialogs. Without the checkbox, values stay in memory for the current application session. A successful login without the checkbox removes any previously remembered profile; cancelling leaves storage unchanged.
- On Windows, opting in encrypts the entire profile (URL, API key, password) using DPAPI/CurrentUser in `%LOCALAPPDATA%\\CompuMaster\\ShopifyAdministration\\credentials.v1.bin`. Do not include this file in source control, cloud synchronization, or releases. It is bound to the Windows user profile; it does not protect against malware already running as that user.
- A remembered profile is loaded and validated at startup. On failure the login dialog opens. A damaged store is not silently overwritten.
- **Abmelden / Konto wechseln** ends the session, cancels pending retrieval, closes API windows, and clears displayed data. The login dialog is prefilled with the last connection and allows another account/URL or relogin. A remembered profile remains until a successful login without the checkbox.
- Passwords are initially masked and can be toggled with the visibility button. The current UI labels are localized in German.
- Authenticated requests, including pagination, cannot leave the configured HTTPS shop API. HTTP redirects are not followed automatically.

## Architecture and platforms

- `ShopifyAdministration.Core`: UI/platform-independent connection handling, authentication, session lifecycle, HTTP transport, and credential-store contract.
- `ShopifyAdministration.Platform`: Windows DPAPI adapter, explicit OS guards, and provider factory.
- `SessionOnlyCredentialStore`: safe fallback when no secure adapter is available. Persistent storage is unavailable; session-only login remains possible.
- `ShopifyAdministration.Desktop`: Avalonia frontend, toolkit file dialogs and platform-independent XLSX generation.
- `ShopifyAdministration.Data`: shared VB data adapters/models without WinForms references.
- `Shopify-Administration`: Windows-only WinForms compatibility frontend.
- [UI framework decision](docs/ui-framework-decision.md): architecture, secure-store evaluation, platform matrix and test limitations.

## Build and tests

Use the .NET 10 SDK and Windows for the complete application/UI suite:

~~~powershell
dotnet restore Shopify-Administration.sln --locked-mode
dotnet build Shopify-Administration.sln -c Release --no-restore
dotnet test ShopifyAdministration.Tests -c Release --no-build
dotnet test ShopifyAdministration.UiTests -c Release --no-build
~~~

All test projects use NUnit 5, NUnit3TestAdapter, and Microsoft.NET.Test.Sdk, so tests are individually discoverable in IDEs and through `dotnet test`. NUnit asynchronous assertions must be awaited.

The Core/storage tests use fake credentials and a fake HTTP handler. On Windows they also verify real DPAPI encryption in unique temporary directories; those tests are explicitly skipped on other platforms. The Windows STA/nonparallel UI fixtures exercise automatic login, logout, prefilled values, account switching, old-data cleanup, password visibility, and default-off persistence using a fake server/store. Additional Windows tests verify controlled pagination and a real XLSX export/read-back. They do not send operational credentials to Shopify.

GitHub Actions runs the shared Core/storage tests on Windows, Linux, and macOS, the UI/export tests on Windows, and uploads TRX results. Test-platform telemetry is disabled in CI. Successful shared-library tests do not establish Linux/macOS desktop support.

Package versions and compatible transitive pins are maintained in `Directory.Packages.props`; committed per-project lock files make CI restores reproducible. After intentional version edits, run `dotnet restore Shopify-Administration.sln --force-evaluate` and review the lock-file changes. See the [dependency upgrade review](docs/dependency-upgrade-review.md) for licensing, removed unused adapters, and intentional update exceptions.

## Development guidance

See [AGENTS.md](AGENTS.md) for architecture, security, testing, issue tracking, and contribution rules. GitHub-facing text and repository documentation are written in English; application UI text may remain localized.

## License and publication

The application's own source is licensed under [MIT](LICENSE). Dependency licenses apply separately; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md), particularly the LGPL obligations for EPPlus 4.5.3.3.

The public repository starts with a sanitized initial commit without the former private Git history. It contains no operational credentials, local credential store, or build output. Configure your own Shopify connection locally in the login dialog.

## Avalonia desktop development

Run `dotnet run --project ShopifyAdministration.Desktop` on a qualified desktop host. Run `dotnet test ShopifyAdministration.Desktop.Tests` for offline headless NUnit UI tests. `--offline-smoke` opens a test window without reading an operational profile or requesting Shopify data; `--startup-smoke` additionally closes the native test window automatically. Windows/Linux/macOS CI runs the new control-tree tests and native startup separately from the shared tests. Platform-specific installation and signing are tracked in #3/#4.

## Complete read-only retrieval

The Avalonia frontend uses validated GraphQL queries for products/variants, all collections, smart collections and transfers with every line-item page. A refresh is asynchronous, cancellable, shows loading/wait state and only enables export after the complete dataset succeeds. Logout clears and invalidates pending results. Product rows preserve technical variants and parent IDs; export columns use explicit API field names. The API browser intentionally shows one read-only GET response and is labelled accordingly. Read access requires the relevant product/transfer permissions; API/version failures are not interpreted as zero records.

Supplier master and purchase-order qualification remains in #11 because the newly documented purchasing reads do not match the available schema validator. The UI explains this limitation. No purchasing, transfer receipt or inventory mutation is performed. See [the retrieval audit](docs/data-retrieval-audit.md) for the feature inventory, completeness policy, retry/error behavior, export mapping and verification boundaries.
