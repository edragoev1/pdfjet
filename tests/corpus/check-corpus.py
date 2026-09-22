"""Checks the reader against real PDFs: the pdf.js and veraPDF test corpora.

    tests/corpus/fetch-corpora.sh .corpora
    python3 tests/corpus/fetch-links.py .corpora
    python3 tests/corpus/check-corpus.py .corpora [--port go|java|dotnet|swift] [--jobs N] [--only SUBSTRING]

Each PDF is read by the port, which merges the whole of it into a document of
its own and splits its first and its last page into two more. MuPDF, through
PyMuPDF, is the reference. The check fails on a file when:

- the port crashes on it, with a runtime error of its own, or takes longer
  than TIMEOUT seconds;
- MuPDF opens it without repairing it and the port cannot read it;
- the port reads a different number of pages than MuPDF, or a page of a
  different size;
- MuPDF has to repair what the port wrote, or cannot open it;
- a page the port merged or split has a different number, size, crop box,
  rotation or text than the page MuPDF reads in the original.

The text is compared with the original's as MuPDF reads it without the parts
of a document that Merge leaves out: the tagging, whose /ActualText MuPDF
extracts in place of the text drawn, the form fields, whose appearances MuPDF
makes again, and the optional content, whose hidden layers a merge shows.

A file MuPDF has to repair and the port cannot read, or cannot merge, is
counted, not failed: the port is not a repair tool. So is a file in which
MuPDF finds no pages. The files whose difference is understood are
in known-differences.txt, one per line, as "path: reason", and are counted
rather than failed too. A file listed there that passes fails the check, so
that the list does not keep what was fixed.
"""

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
from collections import Counter
from concurrent.futures import ProcessPoolExecutor

import pymupdf

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', '..')
TIMEOUT = 60
# The pages of a document whose text is compared, from the start.
MAX_PAGES = 50
# How far apart two sizes, in points, may be.
TOLERANCE = 0.01
# The entries of the catalog that Merge leaves out and that change the text
# MuPDF extracts.
LEFT_OUT = ['StructTreeRoot', 'MarkInfo', 'AcroForm', 'OCProperties']

pymupdf.TOOLS.mupdf_display_errors(False)
pymupdf.TOOLS.mupdf_display_warnings(False)


def build_port(port, out):
    """Builds the port's harness and returns the command that runs it."""
    if port == 'go':
        exe = os.path.join(out, 'corpus-go')
        subprocess.run(['go', 'build', '-o', exe, './tests/corpus/go'], cwd=ROOT, check=True)
        return [exe]
    if port == 'java':
        # As test-java.sh builds the library: Java 8 class files, no warnings.
        classes = os.path.join(out, 'corpus-java')
        release = ['--release', '8']
        if subprocess.run(['javac', '--release', '8', '-version'], capture_output=True).returncode:
            release = []  # Java 8's javac has no such option.
        sources = []
        for folder in ['', 'barcodes', 'pdf417', 'qrcode', 'datamatrix', 'fonts', 'encryption']:
            path = os.path.join('com', 'pdfjet', folder)
            sources += sorted(os.path.join(path, name) for name in os.listdir(os.path.join(ROOT, path))
                              if name.endswith('.java'))
        sources.append(os.path.join('tests', 'corpus', 'java', 'Corpus.java'))
        subprocess.run(['javac', '-O', '-encoding', 'utf-8'] + release +
                       ['-Xlint', '-Xlint:-options', '-Werror', '-d', classes] + sources,
                       cwd=ROOT, check=True)
        return ['java', '-cp', classes, 'Corpus']
    if port == 'dotnet':
        subprocess.run(['dotnet', 'build', 'tests/corpus/dotnet/Corpus.csproj', '-c', 'release', '-v', 'q',
                        '-nologo', '-p:TreatWarningsAsErrors=true', '--artifacts-path', out],
                       cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
        return ['dotnet', os.path.join(out, 'bin', 'Corpus', 'release', 'Corpus.dll')]
    sys.exit(f'The {port} port has no harness yet.')


def corpus_files(corpora):
    """Returns (path relative to corpora, password) of every PDF of the corpora."""
    files = []
    manifest = os.path.join(corpora, 'pdfjs', 'test', 'test_manifest.json')
    passwords = {}
    with open(manifest, encoding='utf-8') as f:
        for entry in json.load(f):
            if 'password' in entry:
                passwords[os.path.basename(entry['file'])] = entry['password']
    pdfs = os.path.join('pdfjs', 'test', 'pdfs')
    for name in sorted(os.listdir(os.path.join(corpora, pdfs))):
        if name.lower().endswith('.pdf'):
            files.append((os.path.join(pdfs, name), passwords.get(name, '')))
    # The PDFs that pdf.js links to, which fetch-links.py fetches: the ones
    # that are there.
    links = 'pdfjs-links'
    if os.path.isdir(os.path.join(corpora, links)):
        for name in sorted(os.listdir(os.path.join(corpora, links))):
            if name.lower().endswith('.pdf'):
                files.append((os.path.join(links, name), passwords.get(name, '')))
    for dirpath, dirnames, filenames in os.walk(os.path.join(corpora, 'verapdf')):
        dirnames[:] = sorted(d for d in dirnames if d != '.git')
        for name in sorted(filenames):
            if name.lower().endswith('.pdf'):
                files.append((os.path.relpath(os.path.join(dirpath, name), corpora), ''))
    return files


def open_mupdf(path, password):
    """Returns the document MuPDF opens, and whether it had to repair it, or (None, error)."""
    try:
        doc = pymupdf.open(path)
    except Exception as e:
        return None, f'{e}'
    if doc.needs_pass and not doc.authenticate(password):
        return None, 'needs a password'
    return doc, doc.is_repaired


def same_size(a, b):
    return abs(a[0] - b[0]) <= TOLERANCE and abs(a[1] - b[1]) <= TOLERANCE


def page_info(page, text):
    """What a page is compared on."""
    info = {
        'mediabox': tuple(round(v, 2) for v in page.mediabox),
        'cropbox': tuple(round(v, 2) for v in page.cropbox),
        'rotation': page.rotation,
    }
    if text:
        info['text'] = page.get_text()
    return info


def compare_pages(what, original, pages, made, made_pages):
    """Compares the pages of the original with the pages the port made."""
    problems = []
    for i, (n, m) in enumerate(zip(pages, made_pages)):
        a = page_info(original[n], i < MAX_PAGES)
        b = page_info(made[m], i < MAX_PAGES)
        for key in a:
            if a[key] != b[key]:
                problems.append(f'{what} page {n + 1}: {key} differs')
                break
        if len(problems) >= 3:
            break
    return problems


def check(args):
    try:
        return check_file(*args)
    except Exception as e:
        if 'page tree' in str(e) or 'number of pages' in str(e):
            return args[1], 'unreadable', []  # MuPDF cannot find the pages.
        # The exception is not returned: one of PyMuPDF's cannot be sent back
        # from the worker.
        return args[1], 'fail', [f'MuPDF failed: {type(e).__name__}: {e}']


def check_file(corpora, relpath, password, command, out):
    path = os.path.join(corpora, relpath)
    with tempfile.TemporaryDirectory(dir=out) as tmp:
        try:
            run = subprocess.run(command + [path, password, tmp], capture_output=True, timeout=TIMEOUT)
        except subprocess.TimeoutExpired:
            return relpath, 'fail', [f'hangs: more than {TIMEOUT} s']
        try:
            port = json.loads(run.stdout)
        except ValueError:
            stderr = run.stderr.decode(errors='replace').strip().splitlines()
            return relpath, 'fail', [f'crashes: {stderr[-1] if stderr else run.returncode}']
        if port.get('crash'):
            return relpath, 'fail', [f'crashes: {port["crash"]}']

        doc, repaired = open_mupdf(path, password)
        if doc is None or doc.page_count == 0:
            # MuPDF cannot read it; whatever the port does short of a crash is fine.
            return relpath, 'unreadable', []
        refused = port.get('error') or port['made'].get('merged', 'ok') != 'ok'
        if refused and repaired:
            return relpath, 'broken', []
        if port.get('error'):
            return relpath, 'fail', [f'not read: {port["error"]}']
        reference, _ = open_mupdf(path, password)
        catalog = reference.pdf_catalog()
        for key in LEFT_OUT:
            reference.xref_set_key(catalog, key, 'null')

        problems = []
        if port['pages'] != doc.page_count:
            problems.append(f'reads {port["pages"]} pages, MuPDF {doc.page_count}')
        else:
            for i, size in enumerate(port['sizes']):
                box = doc[i].mediabox
                if not same_size(size, (box.width, box.height)):
                    problems.append(f'page {i + 1} is {size[0]:g}x{size[1]:g}, MuPDF '
                                    f'{box.width:g}x{box.height:g}')
                    break

        n = doc.page_count
        expected = {'merged': list(range(n)), 'first': [0], 'last': [n - 1]}
        for what, pages in expected.items():
            if n == 0 and what != 'merged':
                continue
            result = port['made'].get(what)
            if result != 'ok':
                problems.append(f'{what}: {result}')
                continue
            made, made_repaired = open_mupdf(os.path.join(tmp, f'{what}.pdf'), '')
            if made is None:
                problems.append(f'{what}: MuPDF cannot open it: {made_repaired}')
                continue
            if made_repaired:
                problems.append(f'{what}: MuPDF repairs it')
            if made.page_count != len(pages):
                problems.append(f'{what}: {made.page_count} pages, not {len(pages)}')
                continue
            problems += compare_pages(what, reference, pages, made, range(len(pages)))
        return relpath, 'fail' if problems else 'pass', problems


def read_known(path):
    known = {}
    with open(path, encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith('#'):
                name, _, reason = line.partition(': ')
                known[name] = reason
    return known


def category(problem):
    """The kind of a problem, with the file's own numbers and words left out."""
    problem = re.sub(r'page \d+', 'page N', problem)
    problem = re.sub(r'[\d.]+x[\d.]+', 'WxH', problem)
    problem = re.sub(r'\d+ pages', 'N pages', problem)
    return problem.split(':')[0] if ':' in problem else problem


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('corpora')
    parser.add_argument('--port', default='go')
    parser.add_argument('--jobs', type=int, default=os.cpu_count())
    parser.add_argument('--only', default='')
    args = parser.parse_args()

    known = read_known(os.path.join(HERE, 'known-differences.txt'))
    with tempfile.TemporaryDirectory() as out:
        command = build_port(args.port, out)
        files = [f for f in corpus_files(args.corpora) if args.only in f[0]]
        tasks = [(args.corpora, relpath, password, command, out) for relpath, password in files]
        counts = Counter()
        kinds = Counter()
        failed = 0
        with ProcessPoolExecutor(args.jobs) as pool:
            for relpath, result, problems in pool.map(check, tasks, chunksize=4):
                if relpath in known:
                    if result == 'fail':
                        result = 'known'
                    else:
                        problems = [f'is listed in known-differences.txt and no longer fails ({result})']
                        result = 'fail'
                counts[result] += 1
                if result == 'fail':
                    failed += 1
                    kinds.update({category(p) for p in problems})
                    print(f'{relpath}: {"; ".join(problems)}', flush=True)

    print()
    print(f'{len(files)} files: {counts["pass"]} pass, {counts["fail"]} fail, '
          f'{counts["known"]} known differences, {counts["broken"]} that MuPDF repairs '
          f'and the port does not read, {counts["unreadable"]} that MuPDF cannot open.')
    for kind, count in kinds.most_common():
        print(f'  {count:5d}  {kind}')
    sys.exit(1 if failed else 0)


if __name__ == '__main__':
    main()
