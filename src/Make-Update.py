"""Build an update-ready Aster ZIP after running Build.cmd. Uses the bundled Python."""
from pathlib import Path
import hashlib
import json
import re
import zipfile

root = Path(__file__).resolve().parent
source = (root / 'Updates.cs').read_text(encoding='utf-8-sig')
version = re.search(r'CurrentVersion="([0-9]+(?:\.[0-9]+){1,3})"', source).group(1)
paths = [p for p in root.iterdir() if p.is_file() and p.suffix.lower() in {'.cs','.exe','.dll','.cmd','.md','.txt','.ico','.png','.json','.py','.xml'} and p.name != 'release.json']
for name in ('runtime','AirDefense','Video'):
    paths.extend(p for p in (root / name).rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.suffix != '.pyc')
for path in paths:
    if path.is_symlink():
        raise ValueError('Linked package files are not supported')
manifest = {'Schema':1, 'AppId':'aster.desktop', 'Version':version, 'Files':[
    {'Name':p.relative_to(root).as_posix(), 'Size':p.stat().st_size, 'Sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
    for p in sorted(paths)]}
(root / 'release.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
label = version
archive = root.parent / ('Aster-Windows-v' + label + '.zip')
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as bundle:
    for path in paths + [root / 'release.json']:
        bundle.write(path, root.name + '/' + path.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as bundle:
    assert bundle.testzip() is None
print('Created update-ready package:', archive.name)
print('Version:', version, '| Files:',len(paths))
