"""Verify package file hashes and produce a portable archive plus SHA-256 checksum."""
import argparse
import hashlib
import json
import os
import subprocess
import sys
from pathlib import Path
import tarfile
import zipfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('package', type=Path)
args = parser.parse_args()
package = args.package.resolve(strict=True)
if os.name == 'nt' and not str(package).startswith('\\\\?\\'):
    package = Path('\\\\?\\' + str(package))
manifest = json.loads((package / 'files.sha256.json').read_text())
actual = {str(path.relative_to(package)).replace('\\', '/') for path in package.rglob('*') if path.is_file() and path.name != 'files.sha256.json'}
if set(manifest) != actual:
    raise SystemExit('Package inventory differs from its manifest')
for relative, expected in manifest.items():
    file = (package / relative).resolve(strict=True)
    if not file.is_relative_to(package) or hashlib.sha256(file.read_bytes()).hexdigest() != expected:
        raise SystemExit('Package hash mismatch: ' + relative)
if package.name.endswith('linux-x64'):
    archive = package.with_name(package.name + '.tar.gz')
    with tarfile.open(archive, 'w:gz') as output:
        output.add(package, arcname=package.name)
elif sys.platform == 'darwin' and json.loads((package / 'release-info.json').read_text()).get('notarized') is True:
    archive = package.with_name(package.name + '.zip')
    subprocess.run(['/usr/bin/ditto','-c','-k','--sequesterRsrc','--keepParent',str(package),str(archive)],check=True)
else:
    archive = package.with_name(package.name + '.zip')
    with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as output:
        for file in sorted(package.rglob('*')):
            if file.is_file(): output.write(file, str(file.relative_to(package.parent)))
digest = hashlib.sha256(archive.read_bytes()).hexdigest()
archive.with_name(archive.name + '.sha256').write_text(digest + '  ' + archive.name + '\n', encoding='utf-8')
print(archive.name, digest)
