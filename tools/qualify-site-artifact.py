"""Verify local site-state retention and ZIP safety without publishing or contacting a shop."""
import importlib.util
import json
from pathlib import Path
import tempfile
import zipfile
from release_version import key

spec=importlib.util.spec_from_file_location('site_builder',Path(__file__).with_name('prepare-site.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
with tempfile.TemporaryDirectory(prefix='unittests_shopify_site_') as temporary:
    owned=Path(temporary).resolve()
    if not owned.is_relative_to(Path(tempfile.gettempdir()).resolve()) or not owned.name.startswith('unittests_shopify_site_'):
        raise RuntimeError('Test directory ownership validation failed')
    state=owned/'state.zip'
    feed={'schemaVersion':1,'appId':'shopify-administration','releases':[{'version':'2.0.0'}]}
    with zipfile.ZipFile(state,'w') as output:
        output.writestr('app-updates.json',json.dumps(feed));output.writestr('clickonce/stable/ApplicationFiles/1_0_0_0/retained.txt','old version')
    prepared=module.prepare(owned/'site',state)
    if json.loads((prepared/'app-updates.json').read_text())!=feed or not (prepared/'clickonce/stable/ApplicationFiles/1_0_0_0/retained.txt').is_file():
        raise RuntimeError('A documentation rebuild reset live update state')
    for index,name in enumerate(['../outside.txt','C:/outside.txt','..\\outside.txt']):
        bad=owned/f'bad-{index}.zip'
        with zipfile.ZipFile(bad,'w') as output:
            entry=zipfile.ZipInfo();entry.filename=name
            output.writestr(entry,'unsafe')
        try: module.prepare(owned/f'unsafe-{index}',bad)
        except ValueError: pass
        else: raise RuntimeError('Unsafe published-state entry accepted: '+name)
if not (key('1.2.3')==key('v1.2.3.0+build') and key('1.2.3-rc.10')>key('1.2.3-rc.2') and key('1.2.3')>key('1.2.3-preview.9')):
    raise RuntimeError('Site-state version ordering is inconsistent')
print('Local site-state retention, archive boundaries and version ordering verified.')
