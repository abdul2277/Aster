"""Build an update-ready Aster ZIP (release.json manifest) from a staged folder.
Usage: python make_package.py <staged-folder> <version> <output.zip>
The staged folder must hold Aster.exe and its companion files. Personal presets are refused."""
import hashlib, json, sys, zipfile
from pathlib import Path

PERSONAL = {'home-location.json'}

def build(root: Path, version: str, archive: Path):
    paths = sorted(p for p in root.rglob('*') if p.is_file() and '__pycache__' not in p.parts
                   and p.suffix != '.pyc' and p.name != 'release.json')
    for p in paths:
        if p.is_symlink():
            raise ValueError('Linked package files are not supported: %s' % p)
        if p.name in PERSONAL:
            raise ValueError('Personal preset must not be published: %s' % p)
    manifest = {'Schema': 1, 'AppId': 'aster.desktop', 'Version': version, 'Files': [
        {'Name': p.relative_to(root).as_posix(), 'Size': p.stat().st_size,
         'Sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in paths]}
    (root / 'release.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
        for p in paths + [root / 'release.json']:
            z.write(p, root.name + '/' + p.relative_to(root).as_posix())
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
    print('Created', archive.name, '| version', version, '| files', len(paths))

if __name__ == '__main__':
    build(Path(sys.argv[1]).resolve(), sys.argv[2], Path(sys.argv[3]).resolve())
