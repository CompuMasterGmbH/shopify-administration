[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][version]$ManifestVersion,
    [ValidateSet('candidate','stable','preview')][string]$Channel = 'candidate',
    [string]$CertificateFile,
    [string]$CertificatePassword
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'ClickOnce generation is supported only on Windows.' }
$source = (Resolve-Path -LiteralPath $Package).ProviderPath
$destination = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $destination) { throw 'Use a new, empty ClickOnce staging directory.' }
if ($Channel -ne 'candidate' -and -not $CertificateFile) { throw 'Stable/preview ClickOnce manifests require a publisher certificate.' }
$sourceInfo = Get-Content -LiteralPath (Join-Path $source 'app-info.json') -Raw | ConvertFrom-Json
if ($sourceInfo.appId -ne 'shopify-administration' -or $sourceInfo.rid -ne 'win-x64') { throw 'Incorrect package identity or architecture.' }
if ($Channel -ne 'candidate' -and ($sourceInfo.stableApproved -isnot [bool] -or -not $sourceInfo.stableApproved -or $sourceInfo.channel -ne $Channel)) { throw 'Use a reviewed, qualified input package for a production channel.' }
if ($ManifestVersion.Revision -lt 0 -or @($ManifestVersion.Major,$ManifestVersion.Minor,$ManifestVersion.Build,$ManifestVersion.Revision | Where-Object { $_ -lt 0 -or $_ -gt 65535 }).Count) { throw 'ClickOnce requires four bounded numeric components.' }
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$versionFolder = 'ApplicationFiles/' + $ManifestVersion.ToString().Replace('.','_')
$app = Join-Path $destination $versionFolder
New-Item -ItemType Directory -Path $app -Force | Out-Null
Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $app -Recurse -Force
$oldInventory = Join-Path $app 'files.sha256.json'
if (Test-Path -LiteralPath $oldInventory) { Remove-Item -LiteralPath $oldInventory }
Copy-Item -LiteralPath (Join-Path $repository 'assets/generated/app.ico') -Destination (Join-Path $app 'app.ico')
$launcherLicense = Join-Path $app 'licenses/Microsoft.DotNet.Mage-10.0.0'
New-Item -ItemType Directory -Path $launcherLicense -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repository 'docs/licenses/DotNetDeploymentTools-LICENSE.txt') -Destination (Join-Path $launcherLicense 'LICENSE.txt')
$infoPath = Join-Path $app 'app-info.json'
$info = $sourceInfo
if ($info.appId -ne 'shopify-administration' -or $info.rid -ne 'win-x64') { throw 'Incorrect package identity or architecture.' }
$info.channel = $Channel
$info.deploymentType = 'clickonce'
$info.stableApproved = $Channel -ne 'candidate'
$info.signed = [bool]$CertificateFile
[IO.File]::WriteAllText($infoPath, ($info | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $app 'release-info.json'), ($info | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
$licenseDirectory = (Resolve-Path -LiteralPath (Join-Path $app 'licenses')).ProviderPath
$expectedLicenseDirectory = [IO.Path]::GetFullPath((Join-Path $app 'licenses'))
$appPrefix = [IO.Path]::GetFullPath($app).TrimEnd('\') + '\'
if ($licenseDirectory -ne $expectedLicenseDirectory -or -not $licenseDirectory.StartsWith($appPrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Licence staging ownership check failed.' }
Compress-Archive -LiteralPath $licenseDirectory -DestinationPath (Join-Path $app 'third-party-licenses.zip')
# Only the exact, newly copied staging folder is removed after archiving its legal materials.
Remove-Item -LiteralPath $licenseDirectory -Recurse -Force

function Invoke-Mage([string[]]$MageArguments) {
    Push-Location $repository
    try {
        & dotnet mage @MageArguments
        if ($LASTEXITCODE -ne 0) { throw 'ClickOnce manifest tooling failed.' }
    }
    finally { Pop-Location }
}
Push-Location $repository
try {
    Invoke-Mage @('-AddLauncher','ShopifyAdministration.Desktop.exe','-TargetDirectory',$app)
    $applicationManifest = Join-Path $app 'ShopifyAdministration.Desktop.exe.manifest'
    Invoke-Mage @('-New','Application','-Name','Shopify Administration','-Version',$ManifestVersion.ToString(),'-FromDirectory',$app,
        '-ToFile',$applicationManifest,'-Processor','msil','-IconFile','app.ico','-UseManifestForTrust','true','-Publisher','CompuMaster GmbH')
    if ($CertificateFile) {
        Invoke-Mage @('-Sign',$applicationManifest,'-CertFile',$CertificateFile,'-Password',$CertificatePassword,'-Algorithm','sha256RSA')
    }
    $deploymentManifest = Join-Path $destination 'ShopifyAdministration.application'
    $provider = "https://compumastergmbh.github.io/shopify-administration/clickonce/$Channel/ShopifyAdministration.application"
    Push-Location $destination
    try {
        Invoke-Mage @('-New','Deployment','-Name','Shopify Administration','-Version',$ManifestVersion.ToString(),'-AppManifest',
            $applicationManifest,'-AppCodeBase',($versionFolder + '/ShopifyAdministration.Desktop.exe.manifest'),'-ToFile',$deploymentManifest,'-Processor','msil',
            '-Install','true','-Publisher','CompuMaster GmbH','-ProviderURL',$provider,'-SupportURL','https://github.com/CompuMasterGmbH/shopify-administration/issues')
    }
    finally { Pop-Location }
    if ($CertificateFile) {
        Invoke-Mage @('-Sign',$deploymentManifest,'-CertFile',$CertificateFile,'-Password',$CertificatePassword,'-Algorithm','sha256RSA')
        Invoke-Mage @('-Verify',$applicationManifest)
        Invoke-Mage @('-Verify',$deploymentManifest)
    }
    [xml]$applicationXml = Get-Content -LiteralPath $applicationManifest
    [xml]$deploymentXml = Get-Content -LiteralPath $deploymentManifest
    $entry = $applicationXml.SelectSingleNode("//*[local-name()='entryPoint']/*[local-name()='commandLine']")
    if ($null -eq $entry -or -not (Test-Path -LiteralPath (Join-Path $app $entry.file))) { throw 'ClickOnce entry point is missing.' }
    $identity = $deploymentXml.SelectSingleNode("/*[local-name()='assembly']/*[local-name()='assemblyIdentity']")
    if ($identity.version -ne $ManifestVersion.ToString() -or $identity.processorArchitecture -ne 'msil') { throw 'Deployment identity mismatch.' }
    $inventory = [ordered]@{}
    Get-ChildItem -LiteralPath $destination -File -Recurse | Sort-Object FullName | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($destination,$_.FullName).Replace('\','/')
        $inventory[$relative] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $record = [ordered]@{ schemaVersion=1; appId='shopify-administration'; version=$info.version; manifestVersion=$ManifestVersion.ToString();
        channel=$Channel; rid='win-x64'; signed=[bool]$CertificateFile; url=$provider; files=$inventory }
    [IO.File]::WriteAllText((Join-Path $destination 'clickonce-inventory.json'), ($record | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
}
finally { Pop-Location }
Write-Output "ClickOnce staged and verified: $Channel / $ManifestVersion"
