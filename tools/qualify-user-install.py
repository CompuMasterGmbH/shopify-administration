"""Exercise per-user installation/link switching and corrupt-update rejection in a test-owned home."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('package', type=Path)
args = parser.parse_args()
if sys.platform not in ['linux','darwin']:
    raise SystemExit('This qualification runs only on Linux/macOS')
package = args.package.resolve(strict=True)
with tempfile.TemporaryDirectory(prefix='unittests_shopify_install_') as temporary:
    owned = Path(temporary).resolve()
    home = owned / 'home'; home.mkdir()
    profile = home / 'profile-preservation-sentinel.bin'; profile.write_bytes(b'unittests_owned_profile')
    def install(source, success=True):
        result = subprocess.run([sys.executable,str(source/'install-user.py'),'--home',str(home),'--allow-candidate'])
        if (result.returncode == 0) != success:
            raise RuntimeError('Unexpected per-user installation result')
        if profile.read_bytes() != b'unittests_owned_profile':
            raise RuntimeError('Installer changed an unrelated test profile')
    install(package)
    link = home / ('.local/bin/shopify-administration' if sys.platform == 'linux' else 'Applications/Shopify Administration.app')
    first = link.resolve(strict=True)
    if not first.is_relative_to(home):
        raise RuntimeError('Installer launcher escaped the test-owned home')
    next_source = owned / 'next-source'
    if sys.platform == 'darwin':
        subprocess.run(['/usr/bin/ditto',str(package),str(next_source)],check=True)
    else:
        shutil.copytree(package,next_source)
    info = json.loads((next_source/'release-info.json').read_text())
    # This tests staging/version-directory switching, not a real signed N-to-N+1 release qualification.
    info['version'] += '.next' if '-' in info['version'] else '-qualification.next'
    (next_source/'release-info.json').write_text(json.dumps(info),encoding='utf-8')
    manifest = json.loads((next_source/'files.sha256.json').read_text())
    manifest['release-info.json'] = hashlib.sha256((next_source/'release-info.json').read_bytes()).hexdigest()
    (next_source/'files.sha256.json').write_text(json.dumps(manifest),encoding='utf-8')
    install(next_source)
    second = link.resolve(strict=True)
    if first == second or not first.exists():
        raise RuntimeError('Prior version was lost or the launcher did not switch')
    executable = second if sys.platform == 'linux' else second/'Contents/MacOS/ShopifyAdministration.Desktop'
    subprocess.run([str(executable),'--startup-smoke'],check=True,timeout=30)
    candidate = next_source / ('EPPlus.dll' if sys.platform == 'linux' else 'Shopify Administration.app/Contents/MacOS/EPPlus.dll')
    candidate.write_bytes(candidate.read_bytes()+b'unittests_corruption')
    install(next_source,success=False)
    if link.resolve(strict=True) != second:
        raise RuntimeError('Failed candidate changed the installed launcher')
print('Per-user staging, retained versions, profile preservation and corrupt-update rejection verified.')
