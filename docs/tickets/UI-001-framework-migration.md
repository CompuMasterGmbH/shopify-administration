# UI-001: Migrate the desktop UI to Avalonia or WinUI

Status: Open
Created: 2026-10-28
Priority: Planned technical development
Baseline: WinForms desktop application, now targeting .NET 10.

## Goal

Choose and implement the next UI framework without coupling credentials, sessions or API access to a specific frontend again. Evaluate Avalonia for a cross-platform UI and WinUI for a Windows-only UI. The framework decision remains open.

## Existing foundation

- ShopifyAdministration.Core (netstandard2.0) contains connection validation, API transport, login/logout and ICredentialStore, without WinForms or Windows dependencies.
- ShopifyAdministration.Platform (net10.0) encapsulates Windows DPAPI storage; the factory selects it only on Windows.
- SessionOnlyCredentialStore is the safe fallback: session login is available, persistent storage is unavailable, and there is no plaintext fallback.
- The WinForms login dialog is a replaceable adapter. Persistence defaults to off and showing the password requires an explicit user action.
- Windows UI projects, shared libraries and tests are built separately.

## Tasks and acceptance criteria

- [ ] Document the Avalonia/WinUI decision, target platforms, deployment, accessibility, DPI behavior, maintenance and licensing.
- [ ] Introduce a UI-independent login/status/error view model or presenter using the existing core.
- [ ] Move Windows-specific file/shell actions and export dialogs behind UI/platform adapters; inspect shared code for direct OS calls.
- [ ] For Avalonia, evaluate macOS Keychain and Linux Secret Service adapters. A missing or locked secret store must never trigger plaintext persistence.
- [ ] Test initial non-persistence, explicit persistence, server-validated automatic login, logout cancellation and data clearing, prefilled relogin, account/URL switching, password visibility, disabling persistence, damaged/locked storage, cancellation and timeouts.
- [ ] Execute native cryptography APIs only in their matching platform adapters with platform checks; the fallback must start without those APIs.
- [ ] Run core tests on Windows/Linux/macOS and UI/storage integration tests for every supported platform.
- [ ] Define supported .NET 10 API/package versions and consolidate legacy GraphQL/ShopifySharp adapters during functional migration.

Related work: #2 (data retrieval redesign), #3 (release assets), #4 (deployment and update checks).

## Tracker

[GitHub issue #1](https://github.com/CompuMasterGmbH/shopify-administration/issues/1) tracks implementation; this file retains the plan alongside the source.
