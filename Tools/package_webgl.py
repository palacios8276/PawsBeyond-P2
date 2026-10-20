"""Package a successful Unity Web build for itch.io; Python 3, standard library only."""
import argparse
import pathlib
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument('build', type=pathlib.Path, help='Build folder containing index.html')
args = parser.parse_args()
root = pathlib.Path(__file__).resolve().parents[1]
build = args.build.resolve()
if root.name != 'PawsBeyond-P1' or root not in build.parents or build.parent != root / 'Builds':
    parser.error('Use a build directly under this PawsBeyond-P1/Builds directory.')
if not (build / 'index.html').is_file() or not (build / 'Build').is_dir():
    parser.error('Missing index.html or Build/. Run the Unity build successfully first.')
files = [p for p in build.rglob('*') if p.is_file() and not any(
    'DoNotShip' in part or 'ButDontShip' in part for part in p.relative_to(build).parts)]
if len(files) > 1000 or sum(p.stat().st_size for p in files) > 500_000_000:
    parser.error('itch.io extracted file count/total size limit exceeded.')
for path in files:
    if path.stat().st_size > 200_000_000 or len(path.relative_to(build).as_posix()) > 240:
        parser.error('itch.io per-file size/path limit exceeded: ' + str(path))
archive = build.with_name(build.name + '-itch.zip')
if archive.exists():
    parser.error('ZIP already exists; use a newly generated build to preserve previous packages.')
with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED) as output:
    for path in sorted(files):
        output.write(path, path.relative_to(build).as_posix())
with zipfile.ZipFile(archive) as check:
    assert 'index.html' in check.namelist()
    assert check.testzip() is None
print(f'PASS: {archive}\nFiles: {len(files)}; ZIP bytes: {archive.stat().st_size}')
