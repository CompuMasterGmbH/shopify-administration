"""Prepare a local Pages artifact while preserving previously released update metadata and ClickOnce files."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile

REPOSITORY='CompuMasterGmbH/shopify-administration'
ROOT=Path(__file__).resolve().parents[1]

def extract_state(archive, destination):
    destination=destination.resolve()
    with zipfile.ZipFile(archive) as source:
        entries=source.infolist()
        if len(entries)>30000 or sum(item.file_size for item in entries)>900_000_000:
            raise ValueError('Site capacity exceeded; review retention explicitly')
        for item in entries:
            if '\\' in item.filename or ':' in item.filename or not (destination/item.filename).resolve().is_relative_to(destination) or (item.external_attr>>16)&0o170000==0o120000:
                raise ValueError('Unsafe site archive entry')
        source.extractall(destination)

def prepare(destination, supplied_state=None):
    destination=Path(destination).resolve()
    destination.mkdir(parents=True,exist_ok=False)
    if supplied_state:
        extract_state(supplied_state,destination)
    else:
        releases=json.loads(subprocess.check_output(['gh','release','list','--repo',REPOSITORY,'--exclude-drafts','--limit','30','--json','tagName'],text=True))
        for release in releases:
            details=json.loads(subprocess.check_output(['gh','release','view',release['tagName'],'--repo',REPOSITORY,'--json','assets'],text=True))
            if not any(asset['name']=='site-state.zip' for asset in details['assets']):
                continue
            with tempfile.TemporaryDirectory(prefix='shopify_site_state_') as cache:
                subprocess.run(['gh','release','download',release['tagName'],'--repo',REPOSITORY,'--pattern','site-state.zip','--dir',cache],check=True)
                extract_state(Path(cache)/'site-state.zip',destination)
            break
    shutil.copy2(ROOT/'docs/updates/index.html',destination/'index.html')
    if not (destination/'app-updates.json').exists():
        shutil.copy2(ROOT/'docs/updates/app-updates.json',destination/'app-updates.json')
    (destination/'.nojekyll').write_text('',encoding='utf-8')
    return destination

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--published-state',type=Path)
    args=parser.parse_args()
    prepare(args.output,args.published_state)
    print('Local installation-page artifact prepared; no external publication performed.')
