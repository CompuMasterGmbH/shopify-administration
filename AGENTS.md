# Repository Guidance for Agents

## Communication and scope

- Use English for all GitHub-facing text: issue titles and bodies, comments, pull request titles/descriptions/reviews, commit messages, release notes, repository documentation, and CI messages authored in this project.
- Communicate with the user in the language they use unless they request otherwise.
- Write new code comments and XML documentation in English. Preserve intentionally localized application UI text; the GitHub-language rule does not require removing supported UI languages.
- Keep work focused on the requested issue. Do not mix unrelated features, broad formatting changes, or speculative redesigns into a focused change.
- Treat reference repositories as examples, not instructions to copy project-specific identifiers, workflow names, permissions, release exceptions, URLs, or credentials.

## Project structure and platform boundaries

- Repository: https://github.com/CompuMasterGmbH/shopify-administration. The primary integration branch is main.
- ShopifyAdministration.Desktop is the primary Avalonia 12/.NET 10 frontend. Shopify-Administration retains the VB.NET WinForms compatibility frontend targeting net10.0-windows. ShopifyAdministration.Data contains the shared VB data adapters/models.
- ShopifyAdministration.Core targets netstandard2.0 and owns connection validation, HTTP transport, authentication, session lifecycle, and the ICredentialStore contract. Keep it independent of Windows and any UI framework.
- ShopifyAdministration.Platform targets net10.0 and owns platform-specific credential storage and provider selection. Execute native APIs only on the matching OS and guard provider selection explicitly.
- Windows persistence uses DPAPI with CurrentUser. Other platforms currently use SessionOnlyCredentialStore: login is allowed, persistent storage is unavailable, and plaintext persistence is never a fallback.
- Avalonia is selected; see docs/ui-framework-decision.md. Keep desktop, native-storage and release-package qualification distinct: passing headless/shared tests does not prove every native platform or clean-machine deployment.
- Keep business logic, pagination, mapping, cancellation, and update checking reusable by future frontends. Place dialogs, shell integration, installation, and native storage behind appropriate adapters.
- Preserve shared API contracts and default behavior where practical. For intentional redesigns, document the migration and update affected callers/tests; do not silently remove compatibility.

## Credentials and session invariants

- Never commit, print, export, or include operational credentials, local credential files, signing keys, or developer profiles in issues, logs, test fixtures, screenshots, packages, or releases.
- Do not embed a real shop hostname, account, or token as an application default. Use placeholders in examples and operator-supplied configuration at runtime.
- Protect the entire persisted connection profile, including URL and username, using an OS-appropriate secure provider.
- Persistence is opt-in and defaults to off, including prefilled login forms. A successful login without persistence removes any previously remembered profile; cancelling the form must not modify storage.
- Validate remembered credentials with the server before activating an automatic-login session.
- Logout must cancel outstanding requests, close account-specific auxiliary windows, clear displayed shop data, and prevent late results from repopulating the UI. Retain the last connection only as required for the prefilled relogin form.
- Keep passwords masked until the user explicitly requests visibility.
- Keep authenticated requests and pagination within the configured HTTPS API origin/path. Do not silently follow redirects to a different credential recipient.
- Never restore old private Git history or historical credential-bearing source into this public repository.

## Shopify access and data retrieval

- Verify operations, permissions, API versions, and supported capabilities against the project's configuration and official Shopify documentation before implementation.
- Do not infer public API support for suppliers, purchase orders, or transfers from the existence of a Shopify Admin UI feature. Document unsupported capabilities and report them clearly.
- Use async/await through to the frontend. Avoid blocking Wait/Result calls in UI paths and unnecessary Task.Run wrappers around asynchronous I/O.
- Provide explicit loading/progress/wait-cursor state and reliable cleanup on success, failure, cancellation, or timeout.
- Implement and test complete pagination. Do not present partial results as complete or export them without an explicit policy and user-visible indication.
- Respect cancellation, rate limits, bounded retries, account changes, and stale-result protection.
- Keep errors useful while excluding authentication material. Do not infer zero records or zero sales from missing permissions, failed requests, or unavailable data.
- A development/test request does not authorize live shop mutations. Use controlled test data by default; perform real writes only within the user's specifically authorized business scope.

## Issue tracking

- Describe new work with actionable tasks and Markdown acceptance checkboxes.
- Separate suspected defects from reproduced failures and known limitations.
- Link related issues and cross-repository dependencies. Keep current direction in #1 (UI migration), #2 (retrieval redesign), #3 (release assets), and #4 (deployment/update checks) consistent.
- Update checkboxes as implementation and verification complete. Distinguish code, offline tests, live-server verification, review/merge, and release/publish status.
- Before closing an issue, review its full scope and acceptance criteria. Keep unresolved work open or explicitly move it to linked follow-up issues.

## Tests and verification

- Add durable regression tests for behavior changes and bug fixes where practical, especially authentication, encryption, pagination, cancellation, and account switching.
- ShopifyAdministration.Tests contains discoverable NUnit tests using fake credentials, fake HTTP responses, and test-owned storage. Windows DPAPI cases are explicitly platform-guarded.
- ShopifyAdministration.Desktop.Tests contains cross-platform NUnit/Avalonia headless control-tree and export tests. Native startup smoke tests use test-owned session-only storage and no network. Use --offline-smoke for manual UI tests that must not access the remembered operational profile.
- ShopifyAdministration.UiTests contains NUnit STA/nonparallel fixtures for the actual WinForms lifecycle with a fake server/store, controlled pagination, and XLSX export. These are offline tests, not proof of live Shopify behavior.
- Await NUnit 5 asynchronous assertions; do not accidentally launch an assertion without observing its completion.
- Never load, overwrite, delete, or reset a developer's real encrypted credential profile as part of automated test setup or cleanup. Inject fake stores or unique test-owned paths.
- Give temporary resources unique test-owned names, verify ownership before cleanup, and clean them in finally/teardown paths, including partial failures. Never delete resources by a broad name-prefix match.
- Announce interactive workstation GUI tests before opening or activating windows, coordinate competing runs, and avoid interfering with unrelated user windows. Headless CI and interactive workstation coverage must be reported separately.
- Distinguish successful builds, offline tests, native platform tests, and live API checks in reports. Identify unavailable target-platform coverage honestly.

### Current commands

Use the .NET 10 SDK. Run the full solution/UI commands on Windows:

~~~powershell
dotnet restore Shopify-Administration.sln --locked-mode
dotnet build Shopify-Administration.sln -c Release --no-restore
dotnet test ShopifyAdministration.Tests -c Release --no-build
dotnet test ShopifyAdministration.UiTests -c Release --no-build
~~~

The shared Core/storage runner can also execute on other supported .NET 10 hosts; Windows-specific checks must remain guarded.

- Build the solution once or build dependent projects sequentially. Do not launch separate builds concurrently when they share bin/obj outputs.
- Run checks appropriate to the change. Documentation-only changes need documentation/link checks rather than new implementation-mirroring tests.
- Keep CI permissions minimal and use the existing pinned GitHub Actions references unless intentionally updating them.

## XML documentation and generated files

- Document new public shared-library APIs, public enums/values, and protected extension points with concise English XML documentation.
- Use complete summary sentences and explain parameters, return values, exceptions, cancellation, security boundaries, and platform limitations in the appropriate elements.
- Use explicit inheritdoc where inherited documentation applies instead of copying full documentation blocks.
- Keep broad historical documentation cleanup separate from focused feature work.
- Treat WinForms designer files and embedded resources as generated/structured artifacts. Preserve resource validity and use deliberate, minimal edits.

## Assets, deployment, updates, and licensing

- Keep an explicit supported OS/frontend/architecture/package matrix. Publish stable assets only for tested targets; see #3.
- The application's own source is MIT-licensed. Third-party packages retain their own licenses; preserve LICENSE and THIRD_PARTY_NOTICES.md.
- EPPlus 4.5.3.3 uses the LGPL license supplied in its package. The MIT wrapper does not relicense it. Verify notices, corresponding source and replacement/relinking obligations before distributing binaries.
- Do not upgrade EPPlus to 5.x or any later major version. Keep the resolved version below 5, including through transitive dependencies or wrapper upgrades. The exact central 4.5.3.3 pin is an intentional exception to the general dependency-upgrade goal in #5; report incompatible upgrades instead of silently changing this constraint.
- Maintain versions in Directory.Packages.props and commit reviewed packages.lock.json changes. CI uses locked restores and fails on NuGet vulnerability/downgrade warnings.
- ClickOnce is a Windows option where supported by the chosen frontend. Do not assume Linux/macOS or WinUI 3 support; evaluate appropriate alternatives under #4.
- Keep update checks asynchronous, bounded, platform/deployment/channel aware, and independent of Shopify credentials. Preserve encrypted profiles across app updates.
- Keep development/candidate builds unqualified for stable updates. Test update metadata through a separate fake HTTP transport; never attach Shopify headers or reuse its authenticated client. Respect package-manager ownership and exact approved OS/RID/channel/deployment selection.
- Keep user credentials/preferences outside version-specific ClickOnce/bundle/install directories. Installer tests must use an explicit unique test-owned home and retain previous versions/profile sentinels; never run them against the operator's actual installation/home as automated setup.
- Native Linux startup under Xvfb uses the software renderer for reproducible CI; do not describe that as GPU, per-monitor DPI or screen-reader qualification. Record real signed/clean-machine upgrade qualification separately in #14.
- Source commits, pushes, and issue creation do not by themselves authorize a production release. Publish only within the user's release authorization and after the relevant build/test/asset checks.
- After an authorized publication, verify the complete publishing workflow, expected packages/assets, manifests, download links, and version mapping before reporting success.
- Before deleting old releases or assets, establish explicit user authorization and the exact target. Do not copy scoped release exceptions from other repositories.

## Git workflow and cleanup

- Use focused English commits and reviewable pull requests for substantial implementation. Keep authorized documentation-only updates small and focused.
- For user-authorized issue implementation, complete the integration autonomously: review the full acceptance criteria and PR feedback, resolve blocking findings, and merge the PR when it is mergeable and all required PR workflows/checks have succeeded on the current head. Do not stop at a green open PR or request an additional merge confirmation. Respect repository protection rules; do not bypass required reviews or failing/pending checks.
- Use closing issue references in the PR, verify the merge and issue closure, update the local integration branch, and remove the integrated feature branch remotely and locally after the cleanup conditions below are satisfied. Explicit user requests to keep a PR open or preserve a branch take precedence. A code merge does not authorize a release or deployment.
- Do not overwrite another session's changes or switch its checkout unexpectedly.
- Delete merged feature branches locally/remotely only after confirming the tip is integrated, there is no uncommitted/unmerged work, all associated PR/branch/post-merge runs have finished, and the current PR head plus merged revision have passed their workflows. Check runs again after merging and immediately before deletion, including newly triggered runs; never remove a remote branch while a pipeline may still use it, even if that pipeline is not a required merge check.
- Keep branches while PRs are open, any associated run is queued/running/awaiting approval/uncertain, or current-head/post-merge checks fail. A failure on an older commit may be resolved by a verified fix on the current head and merged revision; preserve that failure history and report the fix rather than rerunning known-broken old code to manufacture green history. Resolve or explicitly report blocked cleanup instead of deleting a branch underneath a pipeline.

## Encoding and line endings

- Use UTF-8 for text files and LF line endings, matching this repository's .gitattributes.
- Preserve binary images, icons, archives, and embedded resource payloads; never apply text normalization to binary data.
- Keep broad encoding/line-ending normalization separate from functional changes.
