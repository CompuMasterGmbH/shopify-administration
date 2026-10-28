#!/usr/bin/env python3
"""Install a verified Linux/macOS portable package per-user, retaining prior versions and profiles."""
import argparse
import hashlib
import json
import os
import platform
import re
from pathlib import Path
import shutil
import subprocess
import sys
import uuid

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--home', type=Path, default=Path.home(), help='Explicit test-owned home for qualification, otherwise the current user home')
parser.add_argument('--allow-candidate', action='store_true', help='Permit an isolated unsigned/ad-hoc qualification candidate')
args = parser.parse_args()
if sys.platform not in ['linux', 'darwin']:
    raise SystemExit('Use Windows ClickOnce or the Windows portable installation guide')
source = Path(__file__).resolve().parent
info = json.loads((source / 'release-info.json').read_text())
expected = 'linux-x64' if sys.platform == 'linux' else 'osx-arm64'
if info['appId'] != 'shopify-administration' or info['rid'] != expected:
    raise SystemExit('Package does not match this supported platform/architecture')
if platform.machine().lower() not in (['x86_64','amd64'] if sys.platform == 'linux' else ['arm64','aarch64']):
    raise SystemExit('This CPU architecture is not qualified by the package')
if not re.fullmatch(r'\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?', info['version']):
    raise SystemExit('Invalid package version')
if info['stableApproved'] is not True and not args.allow_candidate:
    raise SystemExit('Unqualified candidate; do not install as a trusted production build')
manifest = json.loads((source / 'files.sha256.json').read_text())
actual = {str(path.relative_to(source)).replace(os.sep,'/') for path in source.rglob('*') if path.is_file() and path != source / 'files.sha256.json'}
if set(manifest) != actual or any(path.is_symlink() for path in source.rglob('*')):
    raise SystemExit('Unexpected or symbolic package files')
for relative, digest in manifest.items():
    file = (source / relative).resolve(strict=True)
    if not file.is_relative_to(source) or file.is_symlink() or hashlib.sha256(file.read_bytes()).hexdigest() != digest:
        raise SystemExit('Package integrity failed before changing the current installation')
home = args.home.resolve()
if any(character in str(home) for character in '\n\r\x00'):
    raise SystemExit('Unsupported home path')
versions = home / ('.local/share/shopify-administration/versions' if sys.platform == 'linux' else 'Library/Application Support/CompuMaster/ShopifyAdministration/installs')
target = versions / info['version']
if target.exists():
    raise SystemExit('This version already exists; it is not overwritten')
if not target.resolve().is_relative_to(versions.resolve()):
    raise SystemExit('Invalid package version path')
if sys.platform == 'linux':
    link = home / '.local/bin/shopify-administration'
    executable = target / 'ShopifyAdministration.Desktop'
else:
    link = home / 'Applications/Shopify Administration.app'
    executable = target / 'Shopify Administration.app'
if link.exists() or link.is_symlink():
    if not link.is_symlink() or not link.resolve().is_relative_to(versions.resolve()):
        raise SystemExit('Existing unrelated installation/launcher preserved; choose a manual migration')
if sys.platform == 'linux':
    desktop = home / '.local/share/applications/de.compumaster.shopify-administration.desktop'
    marker = '# Managed by Shopify Administration installer\n'
    if desktop.exists() and not desktop.read_text().startswith(marker):
        raise SystemExit('Unrelated desktop entry preserved')
    icon = home / '.local/share/icons/hicolor/256x256/apps/de.compumaster.shopify-administration.png'
    if icon.exists() and not desktop.exists():
        raise SystemExit('Unrelated existing application icon preserved')
if sys.platform == 'darwin':
    subprocess.run(['/usr/bin/codesign', '--verify', '--deep', '--strict', str(source / 'Shopify Administration.app')], check=True)
    if info['stableApproved']:
        subprocess.run(['/usr/sbin/spctl', '--assess', '--type', 'execute', str(source / 'Shopify Administration.app')], check=True)
    target.mkdir(parents=True)
    subprocess.run(['/usr/bin/ditto', str(source), str(target)], check=True)
else:
    shutil.copytree(source, target)
    executable.chmod(0o755)
    desktop.parent.mkdir(parents=True, exist_ok=True)
    icon.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source / 'shopify-administration.png', icon)
    command = str(link).replace('\\', '\\\\').replace('"', '\\"').replace('`', '\\`').replace('$', '\\$').replace('%','%%')
    desktop.write_text(marker + '[Desktop Entry]\nType=Application\nName=Shopify Administration\nExec="' + command + '"\nIcon=de.compumaster.shopify-administration\nTerminal=false\nCategories=Office;\n', encoding='utf-8')
link.parent.mkdir(parents=True, exist_ok=True)
temporary = link.with_name(link.name + '.tmp-' + uuid.uuid4().hex)
temporary.symlink_to(executable, target_is_directory=sys.platform == 'darwin')
os.replace(temporary, link)
print('Installed', info['version'], 'at', target, '; previous versions and user profiles retained.')
