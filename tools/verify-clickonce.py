"""Verify ClickOnce file inventories and XML hash/reference chains without installing or changing trust."""
import argparse
import base64
import hashlib
import json
from pathlib import Path
import urllib.parse
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('directory', type=Path)
args = parser.parse_args()
root = args.directory.resolve(strict=True)
record = json.loads((root / 'clickonce-inventory.json').read_text(encoding='utf-8-sig'))
for relative, expected in record['files'].items():
    file = (root / relative).resolve(strict=True)
    if not file.is_relative_to(root) or hashlib.sha256(file.read_bytes()).hexdigest() != expected:
        raise SystemExit('ClickOnce inventory mismatch: ' + relative)
manifests = [root / 'ShopifyAdministration.application', *root.glob('ApplicationFiles/*/*.manifest')]
if len(manifests) != 2:
    raise SystemExit('Expected one application/deployment manifest pair')
for file in manifests:
    manifest = ET.parse(file).getroot()
    for entry in manifest.iter():
        tag = entry.tag.rsplit('}', 1)[-1]
        reference = entry.get('codebase') if tag == 'dependentAssembly' else entry.get('name') if tag == 'file' else None
        if not reference:
            continue
        hash_node = next((node for node in entry if node.tag.rsplit('}', 1)[-1] == 'hash'), None)
        if hash_node is None:
            continue
        target = (file.parent / urllib.parse.unquote(reference).replace('\\', '/')).resolve(strict=True)
        if not target.is_relative_to(root):
            raise SystemExit('ClickOnce reference leaves its deployment')
        method = next(node for node in hash_node.iter() if node.tag.rsplit('}', 1)[-1] == 'DigestMethod').get('Algorithm')
        algorithm = 'sha256' if method.endswith('sha256') else 'sha1' if method.endswith('sha1') else None
        if algorithm is None:
            raise SystemExit('Unsupported manifest hash algorithm')
        expected = next(node for node in hash_node.iter() if node.tag.rsplit('}', 1)[-1] == 'DigestValue').text
        if base64.b64encode(hashlib.new(algorithm, target.read_bytes()).digest()).decode() != expected:
            raise SystemExit('ClickOnce XML hash mismatch: ' + reference)
deployment = ET.parse(manifests[0]).getroot()
provider = next(node for node in deployment.iter() if node.tag.rsplit('}', 1)[-1] == 'deploymentProvider')
if provider.get('codebase') != record['url']:
    raise SystemExit('Deployment provider differs from the intended channel')
if record['channel'] == 'candidate' and record['signed']:
    raise SystemExit('Candidate identity unexpectedly claims a production signature')
print('ClickOnce inventory, channel and XML hash chain verified:', record['manifestVersion'])
