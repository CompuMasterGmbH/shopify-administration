"""Build a checked portable package from an already-published, RID-specific output."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import plistlib
import re
import shutil
import subprocess
import tarfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
import sys

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--publish', type=Path, required=True)
parser.add_argument('--assets', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--rid', choices=['win-x64', 'linux-x64', 'osx-arm64'], required=True)
parser.add_argument('--version', required=True)
parser.add_argument('--epplus-source', type=Path)
parser.add_argument('--source-repository', type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
if not re.fullmatch(r'\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?', args.version):
    raise SystemExit('Expected a three-component version with an optional prerelease suffix')
publish = args.publish.resolve(strict=True)
if not publish.is_dir() or publish == Path(publish.anchor):
    raise SystemExit('Expected an explicit publish directory')
dependencies = json.loads((publish / 'ShopifyAdministration.Desktop.deps.json').read_text())
if not dependencies['runtimeTarget']['name'].endswith('/' + args.rid):
    raise SystemExit('Publish runtime identifier does not match the package target')
runtime_options = json.loads((publish / 'ShopifyAdministration.Desktop.runtimeconfig.json').read_text())['runtimeOptions']
if runtime_options.get('framework') or runtime_options.get('frameworks') or not runtime_options.get('includedFrameworks'):
    raise SystemExit('A self-contained runtime is required')
names = {name.split('/')[0].lower() for name in dependencies['libraries']}
if any(any(term in name for term in ['nunit', 'testplatform', 'testing.platform', 'applicationinsights', 'codecoverage']) for name in names):
    raise SystemExit('Test tooling must not be packaged')
if not any(name == 'EPPlus/4.5.3.3' for name in dependencies['libraries']):
    raise SystemExit('Required EPPlus 4.5.3.3 boundary was not retained')
for required in ['ShopifyAdministration.Desktop.dll', 'ShopifyAdministration.Core.dll', 'ShopifyAdministration.Platform.dll', 'EPPlus.dll']:
    if not (publish / required).is_file():
        raise SystemExit('Missing application dependency: ' + required)
for path in publish.rglob('*'):
    if path.is_symlink() or any(term in path.name.lower() for term in ['credentials', '.pfx', '.p12', '.user', 'secrets', 'appsettings.development']):
        raise SystemExit('Forbidden publish content: ' + path.name)
    if path.is_file() and path.stat().st_size < 100_000_000:
        data = path.read_bytes()
        for content in [data, data.decode('utf-16-le', errors='ignore').encode('utf-8', errors='ignore')]:
            if re.search(rb'(?:shpat_|shpca_|shpss_|ghp_|glpat-)[A-Za-z0-9_-]{20,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----', content):
                raise SystemExit('Credential-like content detected in: ' + path.name)

package = args.output.resolve() / ('shopify-administration-' + args.version + '-' + args.rid)
package.mkdir(parents=True, exist_ok=False)
payload = package
if args.rid == 'osx-arm64':
    bundle = package / 'Shopify Administration.app'
    payload = bundle / 'Contents/MacOS'
    resources = bundle / 'Contents/Resources'
    resources.mkdir(parents=True)
    shutil.copy2(root / 'assets/generated/app.icns', resources / 'app.icns')
    with (bundle / 'Contents/Info.plist').open('wb') as stream:
        plistlib.dump(dict(CFBundleName='Shopify Administration', CFBundleDisplayName='Shopify Administration',
                          CFBundleIdentifier='de.compumaster.shopify-administration', CFBundlePackageType='APPL',
                          CFBundleExecutable='ShopifyAdministration.Desktop', CFBundleIconFile='app.icns',
                          CFBundleVersion=args.version.split('-')[0], CFBundleShortVersionString=args.version.split('-')[0],
                          NSHighResolutionCapable=True, LSMinimumSystemVersion='14.0'), stream)
shutil.copytree(publish, payload, dirs_exist_ok=True, ignore=shutil.ignore_patterns('*.pdb'))
if args.rid != 'win-x64':
    (payload / 'ShopifyAdministration.Desktop').chmod(0o755)
for name in ['LICENSE', 'THIRD_PARTY_NOTICES.md']:
    shutil.copy2(root / name, package / name)
shutil.copy2(root / 'docs/lgpl-replacement.md', package / 'LGPL-REPLACEMENT.md')
shutil.copy2(root / 'docs/licenses/GPL-3.0.txt', package / 'GPL-3.0.txt')
shutil.copy2(root / 'docs/licenses/Inter-OFL.txt', package / 'Inter-OFL.txt')

# Preserve exact upstream licence, copyright and native notices, including licence files embedded in packages.
assets = json.loads(args.assets.read_text(encoding='utf-8-sig'))
folders = [Path(folder) for folder in assets['packageFolders']]
runtime_version = next(item['version'] for item in runtime_options['includedFrameworks'] if item['name'] == 'Microsoft.NETCore.App')
runtime_directory = next((folder / ('microsoft.netcore.app.runtime.' + args.rid) / runtime_version for folder in folders
                          if (folder / ('microsoft.netcore.app.runtime.' + args.rid) / runtime_version).is_dir()), None)
if runtime_directory is None:
    raise SystemExit('Matching self-contained runtime legal materials are missing')
runtime_licenses = package / 'licenses' / ('Microsoft.NETCore.App.Runtime-' + args.rid + '-' + runtime_version)
runtime_licenses.mkdir(parents=True)
for notice in ['LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT']:
    shutil.copy2(runtime_directory / notice, runtime_licenses / notice)
for identity, library in assets['libraries'].items():
    if library.get('type') != 'package':
        continue
    package_dir = next(folder / library['path'] for folder in folders if (folder / library['path']).is_dir())
    target = package / 'licenses' / identity.replace('/', '-')
    for source in package_dir.rglob('*'):
        if source.is_file() and (source.suffix.lower() in ['.txt', '.md', '.license', '.nuspec'] or re.search(r'license|licence|notice|copyright|copying', source.name, re.I)):
            relative = source.relative_to(package_dir)
            if source.stat().st_size > 5_000_000:
                continue
            destination = target / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)

sources = package / 'sources'
sources.mkdir()
provenance = json.loads((root / 'tools/epplus-source.json').read_text())
archive = args.epplus_source.read_bytes() if args.epplus_source else urllib.request.urlopen(provenance['url'], timeout=90).read()
if hashlib.sha256(archive).hexdigest() != provenance['sha256']:
    raise SystemExit('EPPlus corresponding-source archive hash mismatch')
epplus_archive = sources / 'EPPlus-4.5.3.3-source.zip'
epplus_archive.write_bytes(archive)
with zipfile.ZipFile(epplus_archive) as original:
    project = original.read(next(name for name in original.namelist() if name.endswith('/EPPlus/EPPlus.MultiTarget.csproj'))).decode('utf-8-sig')
    document = ET.fromstring(project)
    if document.findtext('.//Version') != '4.5.3.3' or document.findtext('.//AssemblyVersion') != '4.5.3.3':
        raise SystemExit('Corresponding-source version mismatch')
shutil.copy2(root / 'tools/epplus-source.json', sources / 'EPPlus-source-provenance.json')
source_repository = (args.source_repository or root).resolve(strict=True)
origin = subprocess.check_output(['git', '-C', str(source_repository), 'remote', 'get-url', 'origin'], text=True).strip()
if not re.search(r'github\.com[:/]CompuMasterGmbH/shopify-administration(?:\.git)?$', origin, re.I):
    raise SystemExit('Application source repository identity mismatch')
subprocess.run(['git', '-C', str(source_repository), 'archive', '--format=zip', '--output', str(sources / 'application-source.zip'), 'HEAD'], check=True)
commit = subprocess.check_output(['git', '-C', str(source_repository), 'rev-parse', 'HEAD'], text=True).strip()
info = dict(schemaVersion=1, appId='shopify-administration', frontend='avalonia', version=args.version,
            channel='candidate', rid=args.rid, deploymentType='portable', commit=commit,
            selfContained=True, signed=False, notarized=False, stableApproved=False)
(payload / 'app-info.json').write_text(json.dumps(info, indent=2) + '\n', encoding='utf-8')
(package / 'release-info.json').write_text(json.dumps(info, indent=2) + '\n', encoding='utf-8')
if args.rid == 'osx-arm64' and sys.platform == 'darwin':
    subprocess.run(['codesign', '--force', '--deep', '--sign', '-', str(bundle)], check=True)
    subprocess.run(['codesign', '--verify', '--deep', '--strict', str(bundle)], check=True)
if args.rid == 'linux-x64':
    shutil.copy2(root / 'assets/generated/app-256.png', package / 'shopify-administration.png')
    (package / 'shopify-administration.desktop').write_text('[Desktop Entry]\nType=Application\nName=Shopify Administration\nExec=ShopifyAdministration.Desktop\nIcon=shopify-administration\nTerminal=false\nCategories=Office;\n', encoding='utf-8')
manifest = {str(path.relative_to(package)).replace(os.sep, '/'): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in sorted(package.rglob('*')) if path.is_file()}
(package / 'files.sha256.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(package=str(package), files=len(manifest), version=args.version, rid=args.rid)))
