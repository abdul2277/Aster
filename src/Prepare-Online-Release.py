"""Prepare files for an HTTPS update host. Does not upload or publish anything."""
import argparse
import hashlib
import json
from pathlib import Path
from urllib.parse import urlsplit
import zipfile

def prepare(package, destination, package_url):
    url = urlsplit(package_url)
    if url.scheme != 'https' or not url.netloc or url.username or url.password or url.fragment:
        raise ValueError('Supply the final public HTTPS ZIP address without credentials or a fragment.')
    destination.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(package) as source:
        manifests = [n for n in source.namelist() if n.endswith('/release.json') or n == 'release.json']
        if len(manifests) != 1:
            raise ValueError('Expected one release manifest')
        prefix = manifests[0][:-len('release.json')]
        manifest = json.loads(source.read(manifests[0]))
        # A public release must never carry the owner's home preset or place dictionary.
        files = {}
        for entry in manifest['Files']:
            name = entry['Name']
            if name == 'home-location.json':
                continue
            content = source.read(prefix + name)
            if len(content) != entry['Size'] or hashlib.sha256(content).hexdigest().lower() != entry['Sha256'].lower():
                raise ValueError('Input package integrity failed: ' + name)
            files[name] = b'{"places": []}\n' if name == 'AirDefense/places.json' else content
        manifest['Files'] = [{'Name': n, 'Size': len(b), 'Sha256': hashlib.sha256(b).hexdigest()} for n, b in sorted(files.items())]
        archive = destination / ('Aster-public-' + manifest['Version'] + '.zip')
        with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as output:
            for name, content in files.items():
                output.writestr(prefix + name, content)
            output.writestr(prefix + 'release.json', json.dumps(manifest, indent=2))
    payload = archive.read_bytes()
    feed = {'Schema': 1, 'AppId': 'aster.desktop', 'Version': manifest['Version'],
            'PackageUrl': package_url, 'Size': len(payload), 'Sha256': hashlib.sha256(payload).hexdigest(),
            'Notes': 'Aster ' + manifest['Version'] + '. See the bundled release notes for changes.'}
    (destination / 'latest.json').write_text(json.dumps(feed, indent=2), encoding='utf-8')
    print('Prepared:', archive, 'and', destination / 'latest.json')
    print('Nothing has been uploaded. Host both files over HTTPS and connect the latest.json address in Aster.')
    return archive

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package', type=Path)
    parser.add_argument('destination', type=Path)
    parser.add_argument('--package-url', required=True, help='Final HTTPS URL where the generated public ZIP will be hosted')
    args = parser.parse_args()
    prepare(args.package, args.destination, args.package_url)
