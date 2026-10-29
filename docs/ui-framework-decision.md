# Desktop framework decision

## Decision

Use the MIT-licensed open-source Avalonia 12.1.4 desktop stack on .NET 10. WinUI 3 is a Windows-only framework and does not meet the goal of reusing the desktop UI on other operating systems. No Avalonia Pro, XPF, commercial TreeDataGrid or licence key is required. Editable data grids are a planned product requirement tracked in [#17](https://github.com/CompuMasterGmbH/shopify-administration/issues/17). Keep Avalonia as the desktop framework and evaluate the grid against that requirement. The separate MIT DataGrid 12.1.2 already supports editing, although the application currently uses it for read-only data. Its deprecated, bug-fixes-only maintenance status requires an explicit editing proof and maintenance decision before it is adopted for the new workflow.

- [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)
- [Avalonia MIT licence](https://github.com/AvaloniaUI/Avalonia/blob/12.1.4/licence.md)
- [WinUI platform scope](https://learn.microsoft.com/windows/apps/winui)
- [Avalonia DataGrid status](https://www.nuget.org/packages/Avalonia.Controls.DataGrid/12.1.2)

## Editable grid requirement

The existing DataGrid provides editing support, including editable columns/templates and edit lifecycle events. Editing therefore does not by itself require a framework change or commercial component. Start the proof in #17 with the pinned MIT control. Upstream recommends TreeDataGrid for advanced editing; that recommendation is not a project dependency or licence approval. If the existing control cannot meet the tested workflow, compare maintained open-source alternatives, their exact licences and migration effort. Advanced grouping remains a separate optional requirement.

The current `RecordTable`/`object[]` rows are flattened read/export snapshots. Removing `IsReadOnly` would not establish correct write-back. Add typed drafts with stable entity identities, original values, field-specific validation and dirty state, separate from the saved snapshot. Keep this model and save orchestration independent of the grid so a later control replacement does not require rewriting business rules.

Define an explicit editable-field allowlist and verify the corresponding API operations/permissions. Provide visible changes, Save/Discard, asynchronous progress, conflict and partial-failure handling, and server reconciliation. Cell commit updates the draft; it must not silently issue a server write. Unsaved changes require defined handling during refresh, navigation, export, logout and account switching. Keep unsupported/derived fields read-only; supplier/purchasing capabilities remain dependent on #11.

The grid proof must exercise typed editing, validation, keyboard commit/cancel, virtualization and error recovery on the supported native targets. NUnit/fake HTTP tests cover the save lifecycle and stale-account protection. Record actual native keyboard/DPI/accessibility qualification separately from headless results. Test writes require a specifically authorized test dataset; this architectural requirement does not authorize changing operational shop data.

- [Avalonia DataGrid capabilities and editing](https://docs.avaloniaui.net/controls/data-display/structured-data/datagrid)

## Structure and feature parity

`ShopifyAdministration.Desktop` is the new primary frontend. Login, server-validated remembered login, logout/account switching, products, collections, smart collections, the read-only single-response API browser and XLSX export use the same core/session and existing data models. No former supplier/purchasing placeholder is presented as an implemented operation. Retrieval correctness and purchasing capabilities remain the scope of #2.

`LoginPresenter` is toolkit/OS independent, owns editable login and operation state, and defaults to masked secrets and no persistence on every prefill. `ShopifyAdministration.Data` compiles the existing VB models and retrieval adapters without Windows Forms; the legacy frontend references this library rather than keeping a second implementation. The WinForms frontend remains a compatibility/testing fallback until release/install migration is complete. Its continued existence does not change the framework decision.

Save dialogs use Avalonia's storage provider, behind `DesktopFiles`. Spreadsheet generation produces bytes without OS calls. The new frontend does not shell-launch generated files or invoke Windows services on other platforms. EPPlus stays exactly on 4.5.3.3, below the explicit version 5 boundary.

## Secure storage evaluation

Windows reuses the existing DPAPI CurrentUser adapter and profile location; the frontend does not move, reencrypt or reset operational profiles. Native cryptography is selected only on Windows. Damaged or inaccessible storage returns to editable login with a safe error.

macOS Keychain and Linux Secret Service are suitable future encrypted-storage backends, but require explicit application identity, keychain/collection access policy, lock/permission handling and native integration tests. No such backend is claimed as implemented here. Both platforms intentionally use `SessionOnlyCredentialStore`: login is available, persistence is disabled with an explanation, and plaintext is never a fallback. This is a supported safe behavior, including on headless/locked-secret-service hosts. A separate native-store enhancement may add persistence after verifying those contracts.

## Platform and deployment qualification

| Target | Frontend | Storage | Qualification in this change | Deployment work |
| --- | --- | --- | --- | --- |
| Windows x64 | Avalonia/.NET 10 | DPAPI | NUnit control-tree tests, native startup, existing Windows DPAPI tests | #3 assets; #4 ClickOnce proof |
| Linux x64/X11 | Avalonia/.NET 10 | Session only | NUnit control-tree tests and native startup under Xvfb in CI | #3 dependency/package qualification |
| macOS ARM64 | Avalonia/.NET 10 | Session only | NUnit control-tree tests and native startup in CI | #3 app bundle/signing qualification |
| Windows ARM64, Linux ARM64, macOS x64 | Avalonia candidate | Platform appropriate fallback | Not yet validated as desktop release targets | Evaluate in #3; do not advertise stable assets |

CI operating-system images prove the tested image/architecture, not every version listed in Avalonia's platform tiers. Windows 10 22H2/11, .NET 10-compatible Linux/X11 and supported macOS versions must follow both .NET and Avalonia runtime requirements. Neither a successful cross-build nor headless tests prove clean-machine installation or accessibility conformance. Distribution, signing/notarization and upgrade tests are covered by #3/#4; no production publication is authorized by migration work.

## Accessibility, DPI and tests

Use native Avalonia text fields/buttons/checkboxes, keyboard tab navigation, a default sign-in action, explicit labels and a scrollable login layout, with minimum window size and device-independent sizing. Fluent uses the system theme. Loading is visible and uses a wait cursor. Product data is currently read-only; the planned editing workflow is tracked in #17. Password visibility requires a dedicated action and resets when returning to login.

NUnit headless tests exercise real controls and bindings on Windows/Linux/macOS; presenter tests cover opt-in persistence, auto-login validation, account switching, cancellation, timeout, damaged storage and storage-write failures. The native `--startup-smoke` mode opens and closes the actual platform window and never creates the operational store or performs network requests. `--offline-smoke` leaves that isolated window open for manual layout/keyboard/DPI checks. Interactive screen-reader and per-monitor DPI checks remain a manual qualification step; no accessibility certification is claimed.

## Maintenance and package policy

Pin the Avalonia package family centrally and keep reviewed lock files. Keep API/session/security behavior in Core, data mapping outside UI, and platform-specific services behind adapters. NUnit remains the test framework, including Avalonia.Headless.NUnit; no separate assertion runner is introduced. All new packages and native redistributables retain their own licence notices. See the resolved licence inventory and THIRD_PARTY_NOTICES.md.
