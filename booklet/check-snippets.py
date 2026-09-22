"""Checks the PDFs of the booklet snippets, which booklet/check-snippets.sh writes.

    python3 booklet/check-snippets.py build/check-snippets

The folder has a folder of PDFs for each port, java/out, csharp/out, go/out
and swift/out. The snippets of every port must write the same files as the
Java snippets, with the same number of pages, and the same text on each page:
every run of text in the same font and size at the same place. The PDF/UA and
PDF/A snippets are checked with veraPDF, run as "verapdf" or as the command in
the VERAPDF environment variable, when it is there.
"""

import os
import shutil
import subprocess
import sys

import pymupdf

PORTS = ['java', 'csharp', 'go', 'swift']
PASSWORDS = {'encrypted.pdf': 'hello'}
PROFILES = {'accessible.pdf': 'ua1', 'archival.pdf': '2b'}


def spans(path):
    """Returns the page count and, for every page, its runs of text."""
    doc = pymupdf.open(path)
    if doc.needs_pass:
        doc.authenticate(PASSWORDS[os.path.basename(path)])
    pages = []
    for page in doc:
        runs = []
        for block in page.get_text('dict')['blocks']:
            for line in block.get('lines', []):
                for span in line['spans']:
                    runs.append((span['text'], span['font'], round(span['size'], 1),
                                 tuple(round(v, 1) for v in span['bbox'])))
        pages.append(runs)
    return doc.page_count, pages


def main():
    work = sys.argv[1]
    problems = []
    java_dir = os.path.join(work, 'java', 'out')
    names = sorted(name for name in os.listdir(java_dir) if name.endswith('.pdf'))
    for port in PORTS[1:]:
        port_dir = os.path.join(work, port, 'out')
        port_names = sorted(name for name in os.listdir(port_dir) if name.endswith('.pdf'))
        for name in sorted(set(names) ^ set(port_names)):
            problems.append(f'{port}: {name} is written by only one of {port} and java')
        for name in sorted(set(names) & set(port_names)):
            java_count, java_pages = spans(os.path.join(java_dir, name))
            count, pages = spans(os.path.join(port_dir, name))
            if count != java_count:
                problems.append(f'{port} {name}: {count} pages, Java has {java_count}')
                continue
            for i, (a, b) in enumerate(zip(java_pages, pages), 1):
                if a != b:
                    diff = next(((x, y) for x, y in zip(a, b) if x != y), (a[len(b):] or None, b[len(a):] or None))
                    problems.append(f'{port} {name} page {i}: Java draws {diff[0]}, {port} draws {diff[1]}')
                    break
    print(f'Compared the {len(names)} PDFs of the snippets in every port with Java.')

    verapdf = os.environ.get('VERAPDF', 'verapdf')
    if shutil.which(verapdf):
        for name, profile in PROFILES.items():
            paths = [os.path.join(work, port, 'out', name) for port in PORTS]
            result = subprocess.run([verapdf, '--flavour', profile, '--format', 'text', *paths],
                                    capture_output=True, text=True)
            for line in result.stdout.splitlines():
                if not line.startswith('PASS'):
                    problems.append(f'veraPDF {profile}: {line}')
            print(f'Checked {name} of every port with veraPDF {profile}.')
    else:
        print('veraPDF was not found, so the PDF/UA and PDF/A snippets were not checked with it.')

    for problem in problems:
        print(f'::error::{problem}')
    if problems:
        print(f'{len(problems)} problem{"" if len(problems) == 1 else "s"} found.')
        sys.exit(1)
    print('No problems found.')


if __name__ == '__main__':
    main()
