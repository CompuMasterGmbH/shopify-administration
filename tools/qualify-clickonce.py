"""Check two candidate manifest identities and reject corruption without installing or importing trust."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('first',type=Path)
parser.add_argument('second',type=Path)
args=parser.parse_args()
tool=Path(__file__).with_name('verify-clickonce.py')
identities=[]
for root in [args.first,args.second]:
    subprocess.run([sys.executable,str(tool),str(root)],check=True)
    record=json.loads((root/'clickonce-inventory.json').read_text())
    if record['channel']!='candidate' or record['signed']:
        raise SystemExit('Qualification must not publish or trust a production installer')
    document=ET.parse(root/'ShopifyAdministration.application').getroot()
    identity=next(item for item in document if item.tag.rsplit('}',1)[-1]=='assemblyIdentity')
    identities.append(dict(identity.attrib))
if tuple(map(int,identities[1]['version'].split('.'))) <= tuple(map(int,identities[0]['version'].split('.'))):
    raise SystemExit('Expected increasing ClickOnce deployment versions')
for field in ['name','publicKeyToken','processorArchitecture']:
    if identities[0].get(field)!=identities[1].get(field):
        raise SystemExit('ClickOnce application identity changed during the candidate update')
file=next(args.second.glob('ApplicationFiles/*/app-info.json'))
original=file.read_bytes()
try:
    file.write_bytes(original+b'unittests_corruption')
    result=subprocess.run([sys.executable,str(tool),str(args.second)],capture_output=True)
    if result.returncode==0:
        raise SystemExit('Corrupt update was accepted')
finally:
    file.write_bytes(original)
subprocess.run([sys.executable,str(tool),str(args.second)],check=True)
print('Candidate N/N+1 manifest identity and corruption recovery verified; real installation qualification remains separate.')
