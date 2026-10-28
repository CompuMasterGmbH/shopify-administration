"""Generate local update/site state only after every asset of an existing published release is verified."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import tempfile
import zipfile
import xml.etree.ElementTree as ET
from release_version import key as version_key

ROOT=Path(__file__).resolve().parents[1]
REPOSITORY='CompuMasterGmbH/shopify-administration'
spec=importlib.util.spec_from_file_location('site_builder',ROOT/'tools/prepare-site.py')
site_builder=importlib.util.module_from_spec(spec); spec.loader.exec_module(site_builder)

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tag',required=True)
parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args()
if not re.fullmatch(r'v\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?',args.tag):
    raise SystemExit('Use the qualified v-prefixed version tag')
version=args.tag[1:]
release=json.loads(subprocess.check_output(['gh','api',f'repos/{REPOSITORY}/releases/tags/{args.tag}'],text=True))
if release.get('draft') is not False:
    raise SystemExit('Update activation requires an already-published release')
assets={asset['name']:asset for asset in release['assets']}
metadata_names=[name for name in assets if re.fullmatch(r'release-metadata-(win-x64|linux-x64|osx-arm64)\.json',name)]
if not metadata_names:
    raise SystemExit('No qualified release metadata; the existing update feed is retained')
commit=subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD'],text=True).strip()
with tempfile.TemporaryDirectory(prefix='shopify_release_site_') as cache_name:
    cache=Path(cache_name)
    verified=[]; clickonce=[]
    def download(name):
        if name not in assets:
            raise ValueError('Referenced release asset is unavailable')
        target=cache/name
        if not target.exists():
            subprocess.run(['gh','release','download',args.tag,'--repo',REPOSITORY,'--pattern',name,'--dir',str(cache)],check=True)
        return target
    for name in metadata_names:
        file=download(name)
        if file.stat().st_size>131072:
            raise SystemExit('Release metadata exceeds its bound')
        metadata=json.loads(file.read_text(encoding='utf-8-sig'))
        if metadata.get('schemaVersion')!=1 or metadata.get('appId')!='shopify-administration' or metadata.get('version')!=version or metadata.get('commit')!=commit:
            raise SystemExit('Release metadata does not match the exact published source revision')
        channel=metadata.get('channel')
        if channel not in ['stable','preview'] or (channel=='stable' and ('-' in version or release.get('prerelease'))):
            raise SystemExit('Release channel is inconsistent')
        for entry in metadata.get('entries',[]):
            rid=entry.get('rid'); deployment=entry.get('deploymentType')
            if rid not in ['win-x64','linux-x64','osx-arm64'] or entry.get('approved') is not True or entry.get('version')!=version or entry.get('channel')!=channel:
                raise SystemExit('Unqualified release target')
            if not re.fullmatch('[0-9a-fA-F]{64}',entry.get('sha256','')):
                raise SystemExit('Missing release checksum')
            if deployment=='portable':
                expected=f'shopify-administration-{version}-{rid}'+('.tar.gz' if rid=='linux-x64' else '.zip')
                url=f'https://github.com/{REPOSITORY}/releases/download/{args.tag}/{expected}'
                if entry.get('asset')!=expected or entry.get('url')!=url:
                    raise SystemExit('Download destination does not match its OS/architecture/version')
                if hashlib.sha256(download(expected).read_bytes()).hexdigest()!=entry['sha256'].lower():
                    raise SystemExit('Published download hash mismatch')
            elif deployment=='clickonce' and rid=='win-x64' and entry.get('signed') is True:
                expected=f'shopify-administration-{version}-win-x64-clickonce.zip'
                if entry.get('asset')!=expected or entry.get('url')!=f'https://compumastergmbh.github.io/shopify-administration/clickonce/{channel}/ShopifyAdministration.application':
                    raise SystemExit('ClickOnce destination does not match the intended channel')
                archive=download(expected)
                if hashlib.sha256(archive.read_bytes()).hexdigest()!=entry.get('archiveSha256'):
                    raise SystemExit('ClickOnce archive checksum mismatch')
                with zipfile.ZipFile(archive) as contents:
                    if hashlib.sha256(contents.read('ShopifyAdministration.application')).hexdigest()!=entry['sha256'].lower():
                        raise SystemExit('ClickOnce manifest checksum mismatch')
                clickonce.append((archive,channel,entry))
            else:
                raise SystemExit('Unsupported deployment target')
            verified.append({key:entry[key] for key in ['version','channel','rid','deploymentType','approved','sha256','url']} | {'signed':entry.get('signed',False)})
    if not verified:
        raise SystemExit('No verified update entries')
    output=site_builder.prepare(args.output)
    feed=json.loads((output/'app-updates.json').read_text())
    updates=[]; scopes=set()
    for entry in verified:
        scope=(entry['channel'],entry['rid'],entry['deploymentType'])
        if scope in scopes:
            raise SystemExit('Duplicate release deployment scope')
        scopes.add(scope)
        previous=[item for item in feed.get('releases',[]) if (item.get('channel'),item.get('rid'),item.get('deploymentType'))==scope]
        if len(previous)>1:
            raise SystemExit('Ambiguous existing feed scope')
        if previous:
            order=(version_key(entry['version'])>version_key(previous[0]['version']))-(version_key(entry['version'])<version_key(previous[0]['version']))
            if order<0:
                continue
            if order==0 and entry['sha256']!=previous[0]['sha256']:
                raise SystemExit('A released version must not silently change its bytes')
        updates.append(entry)
    selected={(entry['channel'],entry['rid'],entry['deploymentType']) for entry in updates}
    for archive,channel,entry in clickonce:
        if (channel,'win-x64','clickonce') not in selected:
            continue
        target=output/'clickonce'/channel
        target.mkdir(parents=True,exist_ok=True)
        with zipfile.ZipFile(archive) as contents:
            old_manifest=target/'ShopifyAdministration.application'
            if old_manifest.exists():
                old=ET.parse(old_manifest).getroot()
                new=ET.fromstring(contents.read('ShopifyAdministration.application'))
                old_id=next(node for node in old if node.tag.rsplit('}',1)[-1]=='assemblyIdentity')
                new_id=next(node for node in new if node.tag.rsplit('}',1)[-1]=='assemblyIdentity')
                if any(old_id.get(field)!=new_id.get(field) for field in ['name','publicKeyToken','processorArchitecture']):
                    raise SystemExit('ClickOnce identity migration needs explicit qualification')
            for item in contents.infolist():
                existing=target/item.filename
                if item.filename.startswith('ApplicationFiles/') and existing.is_file() and hashlib.sha256(existing.read_bytes()).digest()!=hashlib.sha256(contents.read(item)).digest():
                    raise SystemExit('An existing ClickOnce version directory is immutable')
        # Retain prior immutable ApplicationFiles folders; never remove old payloads implicitly.
        site_builder.extract_state(archive,target)
    feed['releases']=[entry for entry in feed.get('releases',[]) if (entry.get('channel'),entry.get('rid'),entry.get('deploymentType')) not in selected]+updates
    (output/'app-updates.json').write_text(json.dumps(feed,indent=2)+'\n',encoding='utf-8')
    archive=output.parent/'site-state.zip'
    with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED) as snapshot:
        for file in sorted(output.rglob('*')):
            if file.is_file(): snapshot.write(file,str(file.relative_to(output)))
print('Verified local feed/site snapshot generated; no external deployment or release modification performed.')
