"""Fetches the pdf.js test PDFs that are links to a PDF on the web.

    python3 tests/corpus/fetch-links.py [DIR]

DIR is the folder fetch-corpora.sh filled, .corpora by default. Each
pdfjs/test/pdfs/NAME.link file holds the URL of a PDF that pdf.js tests and
does not keep, and the manifest of pdf.js, at the commit fetch-corpora.sh
pins, holds its MD5. The PDF is saved as pdfjs-links/NAME when its MD5 is the
manifest's, so a run reads the same files as the last one, and a file that is
there already is not fetched again. A link that is dead, or whose PDF has
changed, is reported and left out: the check reads the files that are there.
"""

import hashlib
import json
import os
import re
import sys
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor

# How many links are fetched at once, and how often one is tried. The Web
# Archive refuses a client that asks it for many at once, so its links are
# fetched ARCHIVE_WORKERS at a time.
WORKERS = 6
ARCHIVE_WORKERS = 2
ATTEMPTS = 3
TIMEOUT = 60


def original_url(url):
    """Returns the URL of the file the Web Archive keeps, and not of its page.

    Without if_ after its timestamp, a Web Archive URL gives an HTML page with
    the PDF in a frame, as the download script of pdf.js says (issue 8920).
    """
    return re.sub(r'^(https?://web\.archive\.org/web/)(\d+)(/https?://)', r'\1\2if_\3', url)


def md5_of(path):
    with open(path, 'rb') as f:
        return hashlib.md5(f.read()).hexdigest()


def fetch(url, md5, path):
    """Saves the PDF at the URL to the path, and returns None, or why it is not saved."""
    if os.path.exists(path) and md5_of(path) == md5:
        return None
    reason = ''
    for attempt in range(ATTEMPTS):
        if attempt:
            time.sleep(5 * attempt)
        try:
            request = urllib.request.Request(original_url(url), headers={'User-Agent': 'PDFjet corpus check'})
            with urllib.request.urlopen(request, timeout=TIMEOUT) as response:
                data = response.read()
        except Exception as e:
            reason = f'{type(e).__name__}: {e}'
            continue
        if hashlib.md5(data).hexdigest() != md5:
            return 'its MD5 is not the manifest\'s'
        with open(path + '.part', 'wb') as f:
            f.write(data)
        os.replace(path + '.part', path)
        return None
    return reason


def main():
    corpora = sys.argv[1] if len(sys.argv) > 1 else '.corpora'
    pdfs = os.path.join(corpora, 'pdfjs', 'test', 'pdfs')
    with open(os.path.join(corpora, 'pdfjs', 'test', 'test_manifest.json'), encoding='utf-8') as f:
        md5s = {os.path.basename(e['file']): e['md5'] for e in json.load(f) if 'md5' in e}
    out = os.path.join(corpora, 'pdfjs-links')
    os.makedirs(out, exist_ok=True)
    tasks = []
    for name in sorted(os.listdir(pdfs)):
        if name.endswith('.link'):
            pdf = name[:-len('.link')]
            with open(os.path.join(pdfs, name), encoding='utf-8') as f:
                url = f.read().strip()
            if pdf in md5s:
                tasks.append((pdf, url, md5s[pdf]))
    archive = [t for t in tasks if 'web.archive.org' in t[1]]
    others = [t for t in tasks if 'web.archive.org' not in t[1]]
    results = []
    for group, workers in ((others, WORKERS), (archive, ARCHIVE_WORKERS)):
        with ThreadPoolExecutor(workers) as pool:
            results += pool.map(lambda t: (t[0], fetch(t[1], t[2], os.path.join(out, t[0]))), group)
    missing = [(pdf, reason) for pdf, reason in results if reason is not None]
    for pdf, reason in missing:
        print(f'{pdf}: not fetched: {reason}')
    print(f'{len(tasks) - len(missing)} of the {len(tasks)} linked PDFs are in {out}.')


if __name__ == '__main__':
    main()
