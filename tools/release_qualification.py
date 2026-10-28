"""Validate a reviewed production qualification record; never infer release authorization."""
import argparse
import json
from pathlib import Path
import re

RUNNERS = {'win-x64':'windows-latest', 'linux-x64':'ubuntu-latest', 'osx-arm64':'macos-latest'}

def validate(path, version, channel):
    if not re.fullmatch(r'\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?', version) or channel not in ['stable','preview']:
        raise ValueError('Invalid release version/channel')
    record = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    if record.get('schemaVersion') != 1 or record.get('appId') != 'shopify-administration' or record.get('version') != version or record.get('channel') != channel or record.get('approved') is not True:
        raise ValueError('No approved matching release qualification record')
    qualified = []
    for target in record.get('targets', []):
        if target.get('approved') is not True:
            continue
        rid = target.get('rid')
        if rid not in RUNNERS or rid in qualified or not all(target.get(field) is True for field in ['cleanInstallationVerified','upgradeVerified','integrityVerified','nativeUiVerified']):
            raise ValueError('Incomplete or unsupported release target qualification')
        if rid in ['win-x64','osx-arm64'] and target.get('publisherTrustVerified') is not True:
            raise ValueError('Publisher trust qualification is required')
        qualified.append(rid)
    if not qualified:
        raise ValueError('No qualified production target')
    return qualified

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--record', type=Path, required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--channel', choices=['stable','preview'], required=True)
    parser.add_argument('--github-output', type=Path)
    args = parser.parse_args()
    targets = validate(args.record, args.version, args.channel)
    matrix = {'include':[{'rid':rid,'os':RUNNERS[rid]} for rid in targets]}
    print(json.dumps(matrix))
    if args.github_output:
        with args.github_output.open('a', encoding='utf-8') as output:
            output.write('matrix=' + json.dumps(matrix, separators=(',',':')) + '\n')
