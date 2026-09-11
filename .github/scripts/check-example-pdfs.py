"""Checks the example PDFs that the Build workflow's port jobs created.

    python3 check-example-pdfs.py 'DIR/{port}'

The argument is the folder of one port's Example_NN.pdf files, with {port}
standing for java, dotnet, go or swift. The check fails when:

- an example in the C#, Go or Swift port has a different number of pages than
  the Java example, or one of its first MAX_PAGES pages differs from Java's.
  The pages are rendered with PyMuPDF at RESOLUTION dpi and compared pixel by
  pixel, so any visible difference counts. Their text is compared too, every
  run of text with its font, size, color and position, because PyMuPDF draws
  fonts that are not embedded, like the CJK fonts of Example_04, with one
  fallback font, so text in the wrong font can render the same.
- the content stream of any page, its drawing instructions, differs from
  Java's in any token. That catches differences too small to change a pixel,
  like a coordinate written as 511.6 where Java writes 511.59.
- an example that declares PDF/A or PDF/UA compliance fails veraPDF, in any of
  the four ports. The standard comes from the setCompliance call in the Java
  example.

veraPDF is run as "verapdf", or as the command in the VERAPDF environment
variable.
"""

import functools
import hashlib
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from concurrent.futures import ProcessPoolExecutor

import pymupdf

PORTS = ['java', 'dotnet', 'go', 'swift']
EXAMPLES = range(1, 51)
MAX_PAGES = 10
RESOLUTION = 50
EXAMPLES_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'examples')

# Examples a port does not have. See build-swift.sh.
MISSING = {('swift', 30)}

# Examples whose pages a port is allowed to draw differently from Java, with
# the reason, for example: ('go', 4): 'why the Go example draws differently'.
RENDER_EXCEPTIONS = {}


def java_source(n):
    with open(os.path.join(EXAMPLES_DIR, f'Example_{n:02d}.java'), encoding='utf-8') as f:
        return f.read()


def compliance_profile(source):
    """Returns the veraPDF profile of the compliance level the example sets, or None.

    The level is passed to setCompliance or to the PDF constructor. Commented
    out lines are ignored.
    """
    for line in source.splitlines():
        if line.lstrip().startswith('//'):
            continue
        m = re.search(r'\bCompliance\.PDF_(A|UA)_(\d)([A-Z]?)\b', line)
        if m:
            kind, part, level = m.groups()
            return f'ua{part}' if kind == 'UA' else f'{part}{level.lower()}'
    return None


def user_password(source):
    m = re.search(r'setUserPassword\("([^"]*)"\)', source)
    return m.group(1) if m else None


def render(path, password):
    """Returns what compare_example compares.

    That is the page count; the pixels and text spans of the first MAX_PAGES
    pages; and a hash of the content stream tokens of every page. A text span
    is a run of text in one font, size and color, returned as (page number,
    text, font, size, color, bounding box).
    """
    with pymupdf.open(path) as doc:
        if doc.needs_pass and not doc.authenticate(password or ''):
            raise RuntimeError(f'{path}: cannot open, wrong password')
        pages = []
        spans = []
        for i in range(min(doc.page_count, MAX_PAGES)):
            page = doc[i]
            pix = page.get_pixmap(dpi=RESOLUTION)
            pages.append((pix.width, pix.height, pix.samples))
            for block in page.get_text('dict')['blocks']:
                for line in block.get('lines', []):
                    for span in line['spans']:
                        spans.append((i + 1, span['text'], span['font'], round(span['size'], 2),
                                      span['color'], tuple(round(v, 2) for v in span['bbox'])))
        # Only hashes are kept, because Example_43 has thousands of pages.
        content = [hashlib.sha256(b' '.join(doc[i].read_contents().split())).digest()
                   for i in range(doc.page_count)]
        return doc.page_count, pages, spans, content


def difference(a, b):
    (wa, ha, pa), (wb, hb, pb) = a, b
    if (wa, ha) != (wb, hb):
        return f'{wb}x{hb} pixels, Java has {wa}x{ha}'
    changed = sum(1 for i in range(0, len(pa), 3) if pa[i:i + 3] != pb[i:i + 3])
    return f'{changed} of {wa * ha} pixels differ'


def describe_span(span):
    page, text, font, size, color, bbox = span
    return f'page {page} "{text}" in {font} {size}pt, color #{color:06x}, at {bbox}'


def span_difference(java, other):
    for a, b in zip(java, other):
        if a != b:
            return f'draws {describe_span(b)}, Java draws {describe_span(a)}'
    return f'{len(other)} text spans, Java has {len(java)}'


def page_tokens(path, password, i):
    with pymupdf.open(path) as doc:
        if doc.needs_pass:
            doc.authenticate(password or '')
        return doc[i].read_contents().split()


def token_difference(java_path, path, password, i):
    java, other = page_tokens(java_path, password, i), page_tokens(path, password, i)
    for k, (a, b) in enumerate(zip(java, other)):
        if a != b:
            before = b' '.join(java[max(0, k - 6):k]).decode('latin-1')
            return f'writes {b.decode("latin-1")} where Java writes {a.decode("latin-1")}, after "{before}"'
    return f'has {len(other)} tokens, Java has {len(java)}'


def compare_example(template, n):
    """Compares example n of every port with the Java example."""
    problems = []
    name = f'Example_{n:02d}'
    password = user_password(java_source(n))
    java_path = os.path.join(template.format(port='java'), f'{name}.pdf')
    java_count, java_pages, java_spans, java_content = render(java_path, password)
    for port in PORTS[1:]:
        path = os.path.join(template.format(port=port), f'{name}.pdf')
        if (port, n) in MISSING or (port, n) in RENDER_EXCEPTIONS:
            continue
        count, pages, spans, content = render(path, password)
        if count != java_count:
            problems.append(f'{port} {name}: {count} pages, Java has {java_count}')
            continue
        for i, (a, b) in enumerate(zip(java_pages, pages), 1):
            if a != b:
                problems.append(f'{port} {name}: page {i} renders differently from Java ({difference(a, b)})')
        if spans != java_spans:
            problems.append(f'{port} {name}: {span_difference(java_spans, spans)}')
        differing = [i for i, (a, b) in enumerate(zip(java_content, content)) if a != b]
        if differing:
            first = differing[0]
            pages_text = '1 page' if len(differing) == 1 else f'{len(differing)} pages'
            problems.append(f'{port} {name}: the content stream differs from Java on {pages_text}; '
                            f'page {first + 1} {token_difference(java_path, path, password, first)}')
    return problems


def verapdf(profile, paths):
    """Runs veraPDF and returns (path, failures) for every file that fails."""
    command = os.environ.get('VERAPDF', 'verapdf')
    result = subprocess.run([command, '--flavour', profile, '--format', 'mrr', *paths],
                            capture_output=True, text=True)
    try:
        report = ET.fromstring(result.stdout)
    except ET.ParseError:
        raise RuntimeError(f'veraPDF did not produce a report:\n{result.stderr}')
    failed = []
    for job in report.iter('job'):
        path = job.findtext('item/name')
        validation = job.find('validationReport')
        if validation is None:
            failed.append((path, [job.findtext('.//exceptionMessage') or 'veraPDF could not check the file']))
        elif validation.get('isCompliant') != 'true':
            failed.append((path, [
                f"{rule.get('clause')}-{rule.get('testNumber')}: {(rule.findtext('description') or '').strip()}"
                for rule in validation.iter('rule') if rule.get('status') == 'failed'
            ]))
    return failed


def main():
    template = sys.argv[1]
    problems = []

    with ProcessPoolExecutor() as executor:
        for example_problems in executor.map(functools.partial(compare_example, template), EXAMPLES):
            problems += example_problems
    print(f'Rendered up to {MAX_PAGES} pages of each example in every port and compared them, and the '
          f'content streams of all pages, with Java.')

    by_profile = {}
    for n in EXAMPLES:
        profile = compliance_profile(java_source(n))
        if profile:
            by_profile.setdefault(profile, []).append(n)
    for profile, examples in sorted(by_profile.items()):
        port_of = {
            os.path.realpath(os.path.join(template.format(port=port), f'Example_{n:02d}.pdf')): port
            for port in PORTS for n in examples if (port, n) not in MISSING
        }
        for path, failures in verapdf(profile, list(port_of)):
            port = port_of.get(os.path.realpath(path), '?')
            problems.append(f'{port} {os.path.basename(path)}: fails veraPDF {profile} with '
                            f'{len(failures)} failed rules: ' + '; '.join(failures[:5]))
        print(f'Checked {len(port_of)} files with veraPDF {profile}: '
              + ', '.join(f'Example_{n:02d}' for n in examples))

    for problem in problems:
        print(f'::error::{problem}')
    if problems:
        print(f'{len(problems)} problem{"" if len(problems) == 1 else "s"} found.')
        sys.exit(1)
    print('No problems found.')


if __name__ == '__main__':
    main()
