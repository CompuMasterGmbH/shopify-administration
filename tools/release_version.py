"""Numeric/prerelease ordering for release-site state; never compare whole versions lexically."""
import re

def key(value):
    if not isinstance(value,str) or len(value)>128:
        raise ValueError('Invalid release version')
    match=re.fullmatch(r'[vV]?(\d+(?:\.\d+){1,3})(?:-([A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*))?(?:\+[A-Za-z0-9.-]+)?',value)
    if not match:
        raise ValueError('Invalid release version')
    parts=tuple(map(int,match[1].split('.')))
    parts=parts+(0,)*(4-len(parts))
    preview=match[2]
    return parts, (1,) if preview is None else (0,tuple((0,int(item)) if item.isdigit() else (1,item) for item in preview.split('.')))
