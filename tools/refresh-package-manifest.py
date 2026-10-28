"""Recompute a staged package inventory only after approved publisher signing/notarization."""
import argparse
import hashlib
import json
from pathlib import Path
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('package', type=Path)
args = parser.parse_args()
root = args.package.resolve(strict=True)
file = root / 'files.sha256.json'
manifest = {str(path.relative_to(root)).replace('\\','/'):hashlib.sha256(path.read_bytes()).hexdigest()
            for path in sorted(root.rglob('*')) if path.is_file() and path != file}
file.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
