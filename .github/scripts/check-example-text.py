"""Checks that the text of the example PDFs is the text the examples drew.

    python3 check-example-text.py TRACE 'DIR/{port}' [PORT ...]

TRACE is the text-trace.jsonl file that trace-go-examples.sh writes: the Go
examples, built with the texttrace tag, record every string they draw, with
its font and page, before its characters become glyph codes. The second
argument is the folder of one port's Example_NN.pdf files, with {port}
standing for java, dotnet, go or swift; the ports to check follow, all four
when none are given. Every port writes the content streams Java writes, token
for token, which check-example-pdfs.py checks, so what the Go examples drew is
what the examples of every port drew.

Each of the first MAX_PAGES pages of every example of every port is checked.
The characters drawn on the page are compared with the characters extracted
from it, as multisets: the order differs with the layout, right to left text
and wrapping. Both sides are normalized to NFKC, which folds the Arabic
presentation forms that the shaped text is drawn in into their letters and a
ligature into its letters, and the white space and the invisible format
characters, like the bidi marks and the zero width joiners, are left out. The
check fails when:

- MuPDF, through PyMuPDF, extracts other characters than the example drew:
  a character drawn as a space or as another character, one left out or one
  added, like the spaces a core font once drew in place of a quote.
- pdftotext, of Poppler, does the same. It reads the ToUnicode maps and the
  actual text of the spans on its own, apart from MuPDF. It is run with -raw,
  in the order of the content stream: its default mode takes out the hyphen
  at the end of a line, which the examples drew.
- a glyph of the page is the .notdef glyph, glyph 0, of its font, where the
  example drew a character that is not white space: a viewer draws an empty
  box, or nothing, where the character should be.
- a character is drawn in another font than the one the example drew it in,
  like a wrong fallback font. The font is the one PyMuPDF names for the glyph,
  without the subset prefix.
- a character is drawn outside the page, where no viewer shows it: the
  middle of its box is off the page.

The text drawn outside the page is extracted too, and so compared: MuPDF is
not asked to clip it, and pdftotext is given a page larger than any example
page.

The pages and extractors in EXCEPTIONS are not checked, for the reasons given
there. The user password of the encrypted example comes from the Java example,
as in check-example-pdfs.py. pdftotext is run as "pdftotext", or as the
command in the PDFTOTEXT environment variable.
"""

import collections
import json
import os
import re
import subprocess
import sys
import unicodedata
from concurrent.futures import ProcessPoolExecutor

import pymupdf

PORTS = ['java', 'dotnet', 'go', 'swift']
EXAMPLES = range(1, 52)
MAX_PAGES = 10
EXAMPLES_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'examples')

# The text MuPDF extracts: the ligatures are kept, since NFKC takes them apart
# on both sides, and TEXT_MEDIABOX_CLIP, one of the default flags, is left out:
# the text is not clipped to the page.
MUPDF_FLAGS = pymupdf.TEXT_PRESERVE_LIGATURES | pymupdf.TEXT_PRESERVE_WHITESPACE

# At most this many different characters are listed in a problem.
MAX_LISTED = 12

# The checks left out, as {(example, page, check): reason}. The page is a page
# number, or None for every page, and the check is 'mupdf', 'pdftotext',
# 'notdef', 'font' or 'outside', or None for all of them; 'extra' lets MuPDF and
# pdftotext find characters that were not drawn, but not miss any that were.
EXCEPTIONS = {
    (41, 7, 'extra'): 'Pages 7 and 8 are merged from data/testPDFs/rc65-16e.pdf, a form, with its '
                      'text, which the example did not draw.',
    (41, 8, 'extra'): 'Merged from data/testPDFs/rc65-16e.pdf, as page 7.',
    (50, 1, 'extra'): 'The pages are read from data/testPDFs/rc65-16e.pdf, a form, whose labels are '
                      'text; the example draws the answers on the first page.',
    (50, 2, 'extra'): 'Read from data/testPDFs/rc65-16e.pdf, as page 1.',
}


def java_source(n):
    with open(os.path.join(EXAMPLES_DIR, f'Example_{n:02d}.java'), encoding='utf-8') as f:
        return f.read()


def user_password(source):
    m = re.search(r'setUserPassword\("([^"]*)"\)', source)
    return m.group(1) if m else None


def excepted(n, page, check):
    return any(key in EXCEPTIONS for key in [(n, page, check), (n, None, check), (n, page, None), (n, None, None)])


def characters(text):
    """Returns the characters of the text that are compared, after NFKC."""
    return [c for c in unicodedata.normalize('NFKC', text)
            if not c.isspace() and unicodedata.category(c) not in ('Cc', 'Cf', 'Zs', 'Zl', 'Zp')]


def load_trace(path):
    """Returns {pdf file name: {page: [(font, text), ...]}}.

    A page is ('page', number) or ('xref', object number). Pages after
    MAX_PAGES are skipped, since they are not checked.
    """
    trace = collections.defaultdict(lambda: collections.defaultdict(list))
    with open(path, encoding='utf-8') as f:
        for line in f:
            entry = json.loads(line)
            if 'xref' in entry:
                key = ('xref', entry['xref'])
            elif entry['page'] <= MAX_PAGES:
                key = ('page', entry['page'])
            else:
                continue
            trace[entry['pdf']][key].append((entry['font'], entry['text']))
    return {name: dict(pages) for name, pages in trace.items()}


def show(counter):
    """Lists the characters of a Counter compactly, the most frequent first."""
    items = counter.most_common()
    listed = []
    for c, count in items[:MAX_LISTED]:
        shown = c
        if len(c) == 1 and (not c.isprintable() or unicodedata.category(c).startswith('M')):
            shown = f'U+{ord(c):04X}'  # A mark, which would go on the character before it
        listed.append(f'{shown} x{count}' if count > 1 else shown)
    more = f' and {len(items) - MAX_LISTED} more' if len(items) > MAX_LISTED else ''
    return ' '.join(listed) + more


def compare(what, drawn, found, extra_allowed):
    """Returns the problem of an extractor that finds other characters than were drawn."""
    missing, extra = drawn - found, collections.Counter() if extra_allowed else found - drawn
    if not missing and not extra:
        return None
    parts = []
    if missing:
        parts.append(f'misses {sum(missing.values())} drawn: {show(missing)}')
    if extra:
        parts.append(f'finds {sum(extra.values())} not drawn: {show(extra)}')
    return f'{what} ' + '; '.join(parts)


def pdftotext(path, password, pages):
    """Returns the text pdftotext extracts from each of the first pages."""
    command = [os.environ.get('PDFTOTEXT', 'pdftotext'), '-f', '1', '-l', str(pages), '-enc', 'UTF-8',
               '-raw', '-x', '-10000', '-y', '-10000', '-W', '30000', '-H', '30000']
    if password:
        command += ['-upw', password]
    result = subprocess.run(command + [path, '-'], capture_output=True)
    if result.returncode != 0:
        raise RuntimeError(f'pdftotext failed on {path}: {result.stderr.decode(errors="replace")}')
    return result.stdout.decode('utf-8').split('\f')[:pages]


def check_file(path, password, n, pages_drawn):
    """Returns the problems of one PDF file, given what was drawn on its pages."""
    problems = []
    with pymupdf.open(path) as doc:
        if doc.needs_pass and not doc.authenticate(password or ''):
            raise RuntimeError(f'{path}: cannot open, wrong password')
        count = min(doc.page_count, MAX_PAGES)
        xref_page = {doc[i].xref: i + 1 for i in range(doc.page_count)}
        drawn_on = collections.defaultdict(list)
        for (kind, number), texts in pages_drawn.items():
            page = number if kind == 'page' else xref_page.get(number)
            if page is None or page > doc.page_count:
                problems.append(f'text was drawn on {kind} {number}, which the file does not have')
            else:
                drawn_on[page] += texts
        poppler = pdftotext(path, password, count) if count else []
        for i in range(count):
            number = i + 1
            page = doc[i]
            texts = drawn_on.get(number, [])
            drawn = collections.Counter(characters(''.join(text for _, text in texts)))

            def fail(message):
                problems.append(f'page {number}: {message}')

            extra = excepted(n, number, 'extra')
            if not excepted(n, number, 'mupdf'):
                text = page.get_text('text', flags=MUPDF_FLAGS, clip=pymupdf.INFINITE_RECT())
                problem = compare('MuPDF', drawn, collections.Counter(characters(text)), extra)
                if problem:
                    fail(problem)
            if not excepted(n, number, 'pdftotext'):
                problem = compare('pdftotext', drawn, collections.Counter(characters(poppler[i])), extra)
                if problem:
                    fail(problem)

            glyphs = collections.Counter()  # (font, character)
            notdef = collections.Counter()
            outside = collections.Counter()
            for span in page.get_texttrace():
                font = span['font'].split('+', 1)[-1]
                for char in span['chars']:
                    for c in characters(chr(char[0])) if char[0] >= 0 else []:
                        glyphs[font, c] += 1
                        if char[1] == 0:
                            notdef[f'{c} in {font}'] += 1
                        x0, y0, x1, y1 = char[3]
                        if not page.cropbox.contains(pymupdf.Point((x0 + x1) / 2, (y0 + y1) / 2)):
                            outside[c] += 1
            if notdef and not excepted(n, number, 'notdef'):
                fail(f'draws .notdef glyphs for {sum(notdef.values())} characters: {show(notdef)}')
            if outside and not excepted(n, number, 'outside'):
                fail(f'draws {sum(outside.values())} characters outside the page: {show(outside)}')

            if not excepted(n, number, 'font'):
                wanted = collections.Counter()
                for font, text in texts:
                    for c in characters(text):
                        wanted[font, c] += 1
                # A character missing from every font is a problem of the
                # extraction above; here it is one found in another font.
                surplus = glyphs - wanted
                wrong = collections.Counter()
                # The fonts with the fewest glyphs on the page go first, so that
                # a font none of whose characters were drawn in it is blamed
                # before one that lost a character the extraction misses.
                in_font = collections.Counter()
                for (font, c), k in glyphs.items():
                    in_font[font] += k
                for (font, c), k in sorted((wanted - glyphs).items(), key=lambda item: in_font[item[0][0]]):
                    for (other, d), v in list(surplus.items()):
                        if d == c and k > 0 and v > 0:
                            m = min(k, v)
                            wrong[f'{c} in {other}, not {font}'] += m
                            surplus[other, d] -= m
                            k -= m
                if wrong:
                    fail(f'draws {sum(wrong.values())} characters in another font: {show(wrong)}')
    return problems


def check_example(template, trace, ports, n):
    """Checks example n, and the other files it wrote, in every port."""
    name = f'Example_{n:02d}.pdf'
    password = user_password(java_source(n))
    files = sorted({name} | {f for f in trace if re.fullmatch(rf'Example_{n:02d}(_\d+)?\.pdf', f)})
    problems = []
    for port in ports:
        for file in files:
            path = os.path.join(template.format(port=port), file)
            for problem in check_file(path, password, n, trace.get(file, {})):
                problems.append(f'{port} {file[:-4]}: {problem}')
    return problems


def main():
    trace_path, template = sys.argv[1], sys.argv[2]
    ports = sys.argv[3:] or PORTS
    for port in ports:
        if port not in PORTS:
            sys.exit(f'Unknown port {port}; the ports are {", ".join(PORTS)}.')
    trace = load_trace(trace_path)
    problems = []
    with ProcessPoolExecutor() as executor:
        traces = [{f: pages for f, pages in trace.items() if f.startswith(f'Example_{n:02d}')} for n in EXAMPLES]
        for example_problems in executor.map(check_example, [template] * len(EXAMPLES), traces,
                                             [ports] * len(EXAMPLES), EXAMPLES):
            problems += example_problems
    print(f'Compared the text of up to {MAX_PAGES} pages of each example in {", ".join(ports)} '
          f'with the text the examples drew, extracted with MuPDF and pdftotext.')
    for problem in problems:
        print(f'::error::{problem}')
    if problems:
        print(f'{len(problems)} problem{"" if len(problems) == 1 else "s"} found.')
        sys.exit(1)
    print('No problems found.')


if __name__ == '__main__':
    main()
