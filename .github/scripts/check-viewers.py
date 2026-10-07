"""Checks the example PDFs in the engines of the viewers most people use.

    python3 check-viewers.py ENGINE PDF_FOLDER ENCRYPTED_FOLDER OUTPUT_FOLDER

ENGINE is one of:

- pdfium: PDFium, the engine of Chrome and of Android, through pypdfium2.
- pdfjs: pdf.js, the engine of Firefox, in Node, with render-pdfjs/render-pdfjs.mjs.
  Node is run as "node", or as the command in the NODE environment variable,
  and needs "npm ci" run in render-pdfjs first.
- pdfkit: Apple's PDFKit, the engine of Preview, Safari and iOS, with
  render-pdfkit.swift built into the command in the PDFKIT_RENDERER
  environment variable. It runs on macOS only.
- poppler: Poppler, the engine of Evince, Okular and the other Linux desktop
  viewers and of CUPS printing, through its pdftoppm and pdftotext commands.
  pdftoppm draws with Splash, as Okular and CUPS do; Evince draws with Cairo.

PDF_FOLDER has the Java examples, Example_NN.pdf, which the compare job has
checked are the examples of every port. ENCRYPTED_FOLDER has the PDFs that
encrypted-pdfs/main.go writes, with a Cyrillic and a 200 byte password.
Example_30 is opened with its user password and again with its owner
password, the one encrypted PDF with the Cyrillic password and the other with
the 200 byte one. The check fails when, in the engine:

- a PDF does not open, a page does not render, or its text cannot be
  extracted; pdf.js and Poppler do not stop at a page they cannot read, so
  any warning they print counts too.
- a PDF has another number of pages than in MuPDF.
- one of the first MAX_PAGES pages renders nearly blank where MuPDF's does
  not: less than MIN_INK_RATIO of the ink of MuPDF's render at the same
  RESOLUTION, where ink is the fraction of the pixels that are not white.
- one of the first MAX_PAGES pages looks different from MuPDF's render. Both
  are made grey and cut into blocks of SHRINK by SHRINK pixels, and more than
  MAX_DIFFERENCE of the blocks with ink in either render have a mean grey
  more than BLOCK_TOLERANCE apart, from 0 to 255. The engines draw a thin
  line heavier or lighter than MuPDF, and a glyph a pixel apart, which the
  blocks absorb; a table, a chart, an annotation or a line of text left out,
  or drawn elsewhere, they do not.
- a character MuPDF extracts from a page is missing from what the engine
  extracts from it. The characters are compared as multisets, as in
  check-example-text.py, since each engine orders the lines, the words and
  the right to left text in its own way: normalized to NFKC, without the
  white space and the invisible format characters. Every page is checked.

The first MAX_PAGES pages of each PDF, as the engine renders them, and a
contact sheet of each, beside MuPDF's render, are written to OUTPUT_FOLDER
for a person to look at. The largest difference and the smallest ink ratio
found are printed, to see how close the examples come to the limits.

check-examples.sh runs the PDFium, the pdf.js and the Poppler checks; the
PDFKit one runs in the Build workflow only.
"""

import functools
import json
import math
import os
import re
import subprocess
import sys
import tempfile
import unicodedata
from collections import Counter
from concurrent.futures import ProcessPoolExecutor

import pymupdf

ENGINES = {'pdfium': 'PDFium', 'pdfjs': 'pdf.js', 'pdfkit': 'PDFKit', 'poppler': 'Poppler'}
EXAMPLES = range(1, 58)
MAX_PAGES = 10
RESOLUTION = 72
SCRIPTS_DIR = os.path.dirname(os.path.abspath(__file__))
EXAMPLES_DIR = os.path.join(SCRIPTS_DIR, '..', '..', 'examples')

# Calibrated on the examples of Sep 29, 2026, with the pinned versions, on the
# pages RENDER_EXCEPTIONS leaves in. The smallest ink ratio was 0.61 in PDFium,
# 0.55 in pdf.js and 0.43 in Poppler, all on the thin table lines of
# Example_43; a blank page has 0. The most blocks that differ were 2.9% in
# PDFium, on the heavier lines of the tables of Example_13, 1.3% in pdf.js, and
# 3.2% in Poppler, which draws the thin grid lines of the chart of Example_09
# a full pixel wide; the annotations PDFium left out of Example_06, before
# they had appearance streams, were 4.7%.
MIN_INK_RATIO = 0.25
SHRINK = 8
BLOCK_TOLERANCE = 48
MAX_DIFFERENCE = 0.04

# The passwords of the PDFs encrypted-pdfs/main.go writes.
PASSWORDS = {
    'Encrypted_Cyrillic': 'пароль',
    'Encrypted_200_Bytes': '0123456789' * 20,
}

# The PDFs whose pages an engine may draw differently from MuPDF, as (engine,
# name) pairs, with the engine None for every engine, and the reason. The
# check that no page is blank still applies to them.
# None since Oct 7, 2026: Example_04 and 44, whose CJK fonts were not
# embedded, embed IBM Plex Sans JP, KR and SC now, as no example uses the
# Adobe CJK fonts.
RENDER_EXCEPTIONS = {}

# The passwords an engine is given in place of the ones in PASSWORDS, with
# the reason.
PASSWORD_EXCEPTIONS = {
    ('pdfium', 'Encrypted_200_Bytes'): (
        ('0123456789' * 20)[:127],
        'PDFium does not cut a password at 127 bytes, as ISO 32000-2 asks of a viewer, so Chrome opens '
        'the PDF only with the first 127 bytes typed'),
    ('poppler', 'Encrypted_200_Bytes'): (
        'world',
        'pdftoppm and pdftotext keep only the first 32 bytes of a password, so the PDF is opened with its '
        'owner password; Poppler itself cuts a password at 127 bytes, so Evince and Okular open it'),
}


def java_source(n):
    with open(os.path.join(EXAMPLES_DIR, f'Example_{n:02d}.java'), encoding='utf-8') as f:
        return f.read()


def passwords(source):
    """Returns the user and the owner password the example sets, or None."""
    user = re.search(r'setUserPassword\("([^"]*)"\)', source)
    owner = re.search(r'setOwnerPassword\("([^"]*)"\)', source)
    return (user.group(1) if user else None), (owner.group(1) if owner else None)


def files(pdf_dir, encrypted_dir):
    """Returns the PDFs to check, as dictionaries of name, path and password."""
    result = []
    for n in EXAMPLES:
        name = f'Example_{n:02d}'
        path = os.path.join(pdf_dir, f'{name}.pdf')
        user, owner = passwords(java_source(n))
        result.append({'name': name, 'path': path, 'password': user})
        if owner:
            result.append({'name': f'{name}-owner-password', 'path': path, 'password': owner})
    for name, password in PASSWORDS.items():
        result.append({'name': name, 'path': os.path.join(encrypted_dir, f'{name}.pdf'), 'password': password})
    return result


def page_png(out, name, i):
    return os.path.join(out, name, f'page-{i:03d}.png')


def render_pdfium(dpi, max_pages, out, file):
    """Does for PDFium what render-pdfjs.mjs does for pdf.js."""
    import pypdfium2  # Only this engine needs it.
    result = {'pages': 0, 'text': [], 'errors': []}
    try:
        doc = pypdfium2.PdfDocument(file['path'], password=file['password'])
    except pypdfium2.PdfiumError as e:
        result['errors'].append(f'cannot open: {e}')
    else:
        result['pages'] = len(doc)
        os.makedirs(os.path.join(out, file['name']), exist_ok=True)
        for i in range(len(doc)):
            try:
                page = doc[i]
                # With the annotations, as Chrome draws them, on a white page.
                bitmap = page.render(scale=dpi / 72, draw_annots=True, rev_byteorder=True,
                                     fill_color=(255, 255, 255, 255))
                if i < max_pages:
                    row = bitmap.width * bitmap.n_channels
                    data = b''.join(bytes(bitmap.buffer[y * bitmap.stride:y * bitmap.stride + row])
                                    for y in range(bitmap.height))
                    pymupdf.Pixmap(pymupdf.csRGB, bitmap.width, bitmap.height, data, bitmap.n_channels == 4) \
                        .save(page_png(out, file['name'], i + 1))
                result['text'].append(page.get_textpage().get_text_range())
            except pypdfium2.PdfiumError as e:
                result['errors'].append(f'page {i + 1}: {e}')
                result['text'].append('')
        doc.close()
    with open(os.path.join(out, file['name'] + '.json'), 'w', encoding='utf-8') as f:
        json.dump(result, f)


def render_poppler(dpi, max_pages, out, file):
    """Does for Poppler what render-pdfjs.mjs does for pdf.js, with pdftotext
    and pdftoppm. The password is given as the owner and as the user password,
    as a viewer tries the one typed as either."""
    result = {'pages': 0, 'text': [], 'errors': []}
    password = ['-opw', file['password'], '-upw', file['password']] if file['password'] else []

    def run(command, **kwargs):
        done = subprocess.run(command[:1] + password + command[1:], stderr=subprocess.PIPE, **kwargs)
        # Poppler prints what it cannot read of a page and goes on.
        result['errors'] += [f'{command[0]}: {line}' for line in done.stderr.decode(errors='replace').splitlines()]
        return done

    # -raw, in the order of the content stream, keeps the hyphens at the ends
    # of the lines, as check-example-text.py does. Each page ends with a form feed.
    text = run(['pdftotext', '-raw', '-enc', 'UTF-8', file['path'], '-'], stdout=subprocess.PIPE)
    if text.returncode == 0:
        result['text'] = text.stdout.decode('utf-8').split('\f')[:-1]
        result['pages'] = len(result['text'])
    folder = os.path.join(out, file['name'])
    os.makedirs(folder, exist_ok=True)
    if result['pages']:
        run(['pdftoppm', '-png', '-r', str(dpi), '-l', str(max_pages), file['path'], os.path.join(folder, 'poppler')])
        # pdftoppm pads the page numbers to the digits of the last page.
        for png in os.listdir(folder):
            page = re.fullmatch(r'poppler-(\d+)\.png', png)
            if page:
                os.replace(os.path.join(folder, png), page_png(out, file['name'], int(page.group(1))))
    if result['pages'] > max_pages:
        # The other pages are rendered too, to see that they render, but not
        # kept: without a file name pdftoppm writes them to its output, as PPM.
        run(['pdftoppm', '-r', str(dpi), '-f', str(max_pages + 1), file['path']], stdout=subprocess.DEVNULL)
    with open(os.path.join(out, file['name'] + '.json'), 'w', encoding='utf-8') as f:
        json.dump(result, f)


def run_renderer(command, chunk, out):
    """Runs render-pdfjs.mjs or render-pdfkit on some of the files."""
    with tempfile.NamedTemporaryFile('w', suffix='.json', delete=False, encoding='utf-8') as job:
        json.dump({'dpi': RESOLUTION, 'maxPages': MAX_PAGES, 'out': out, 'files': chunk}, job)
    try:
        subprocess.run([*command, job.name], check=True)
    finally:
        os.unlink(job.name)


def render(engine, all_files, out):
    """Renders the files with the engine, in as many processes as there are
    processors, each writing the JSON and the PNG files of its files."""
    all_files = [dict(f, password=PASSWORD_EXCEPTIONS[engine, f['name']][0])
                 if (engine, f['name']) in PASSWORD_EXCEPTIONS else f for f in all_files]
    if engine in ('pdfium', 'poppler'):
        renderer = render_pdfium if engine == 'pdfium' else render_poppler
        with ProcessPoolExecutor() as executor:
            list(executor.map(functools.partial(renderer, RESOLUTION, MAX_PAGES, out), all_files))
        return
    if engine == 'pdfjs':
        command = [os.environ.get('NODE', 'node'), os.path.join(SCRIPTS_DIR, 'render-pdfjs', 'render-pdfjs.mjs')]
    else:
        command = [os.environ['PDFKIT_RENDERER']]
    # The largest files first, dealt out in turn, so the processes finish together.
    by_size = sorted(all_files, key=lambda f: -os.path.getsize(f['path']))
    count = os.cpu_count() or 1
    chunks = [by_size[k::count] for k in range(count) if by_size[k::count]]
    with ProcessPoolExecutor(len(chunks)) as executor:
        list(executor.map(functools.partial(run_renderer, command), chunks, [out] * len(chunks)))


def grey(pix):
    """Returns a render in grey, without the alpha channel of a PNG file: the
    renders are drawn on a white page, which is opaque."""
    if pix.alpha:
        pix = pymupdf.Pixmap(pix, 0)
    if pix.n != 1:
        pix = pymupdf.Pixmap(pymupdf.csGRAY, pix)
    return pix


# Maps a grey value to 1 when it is not white, to count the ink with bytes.count.
INK = bytes(1 if v < 240 else 0 for v in range(256))


def ink(pix):
    return pix.samples.translate(INK).count(1) / (pix.width * pix.height)


def block_difference(a, b):
    """Returns the fraction of the blocks of SHRINK by SHRINK pixels whose mean
    grey differs by more than BLOCK_TOLERANCE, of the blocks with ink in
    either render, or None when the renders differ in size by more than a
    pixel of rounding. A line drawn a little heavier or lighter, or a glyph of
    another font, changes the grey of its blocks a little; a table, a chart, a
    line of text or an image left out or drawn elsewhere changes it a lot."""
    if abs(a.width - b.width) > 1 or abs(a.height - b.height) > 1:
        return None
    width, height = min(a.width, b.width), min(a.height, b.height)
    small = []
    for pix in (a, b):
        pix = pymupdf.Pixmap(pix, width, height, None)
        pix.shrink(int(math.log2(SHRINK)))
        small.append(pix.samples)
    inked = [(x, y) for x, y in zip(*small) if x < 255 or y < 255]
    return sum(1 for x, y in inked if abs(x - y) > BLOCK_TOLERANCE) / len(inked) if inked else 0.0


# In right to left text an opening parenthesis is drawn with the glyph of a
# closing one, and the engines extract it as either, so a pair counts as one.
MIRRORED = str.maketrans(')]}', '([{')


def characters(text):
    """The characters of a text, normalized to NFKC, without the white space
    and the invisible format characters, like the zero width non-joiner."""
    text = unicodedata.normalize('NFKC', text).translate(MIRRORED)
    return Counter(c for c in text if not c.isspace() and unicodedata.category(c) != 'Cf')


def contact_sheet(path, name, engine, pairs):
    """Writes the renders of MuPDF and the engine side by side, three pages to
    a row, at half their size."""
    columns = min(3, len(pairs))
    cell_w = max(p.width for pair in pairs for p in pair) / 2
    cell_h = max(p.height for pair in pairs for p in pair) / 2
    label = 14
    rows = math.ceil(len(pairs) / columns)
    width = columns * (2 * cell_w + 20) + 10
    height = rows * (cell_h + label + 10) + 30
    doc = pymupdf.open()
    sheet = doc.new_page(width=width, height=height)
    sheet.insert_text((10, 20), f'{name}: MuPDF on the left, {ENGINES[engine]} on the right', fontsize=12)
    for k, pair in enumerate(pairs):
        x = 10 + (k % columns) * (2 * cell_w + 20)
        y = 30 + (k // columns) * (cell_h + label + 10)
        sheet.insert_text((x, y + 10), f'page {k + 1}', fontsize=9)
        for j, pix in enumerate(pair):
            rect = pymupdf.Rect(x + j * cell_w, y + label, x + j * cell_w + pix.width / 2, y + label + pix.height / 2)
            sheet.draw_rect(rect, color=(0.6, 0.6, 0.6), width=0.5)
            sheet.insert_image(rect, pixmap=pix)
    sheet.get_pixmap(dpi=72).save(path)
    doc.close()


def check(engine, out, file):
    """Compares what the engine did with a file with MuPDF, and returns the
    problems and the (ink ratio, difference, page) of each rendered page."""
    name = file['name']
    with open(os.path.join(out, name + '.json'), encoding='utf-8') as f:
        result = json.load(f)
    problems = [f'{ENGINES[engine]} {name}: {error}' for error in result['errors']]
    measures = []
    with pymupdf.open(file['path']) as doc:
        if doc.needs_pass and not doc.authenticate(file['password'] or ''):
            return [f'MuPDF {name}: cannot open, wrong password'], measures
        if result['pages'] != doc.page_count:
            problems.append(f'{ENGINES[engine]} {name}: {result["pages"]} pages, MuPDF has {doc.page_count}')
            return problems, measures
        pairs = []
        for i, page in enumerate(doc):
            if i < MAX_PAGES and os.path.exists(page_png(out, name, i + 1)):
                reference = grey(page.get_pixmap(dpi=RESOLUTION))
                rendered = grey(pymupdf.Pixmap(page_png(out, name, i + 1)))
                pairs.append((reference, rendered))
                reference_ink, rendered_ink = ink(reference), ink(rendered)
                ratio = rendered_ink / reference_ink if reference_ink else 1.0
                difference = block_difference(reference, rendered)
                excepted = (engine, name) in RENDER_EXCEPTIONS or (None, name) in RENDER_EXCEPTIONS
                measures.append((ratio, 0.0 if excepted else math.inf if difference is None else difference,
                                 f'{name} page {i + 1}'))
                if ratio < MIN_INK_RATIO:
                    problems.append(f'{ENGINES[engine]} {name}: page {i + 1} renders nearly blank, '
                                    f'{rendered_ink:.2%} of it is ink, in MuPDF {reference_ink:.2%}')
                elif excepted:
                    pass
                elif difference is None:
                    problems.append(f'{ENGINES[engine]} {name}: page {i + 1} renders {rendered.width}x'
                                    f'{rendered.height} pixels, MuPDF {reference.width}x{reference.height}')
                elif difference > MAX_DIFFERENCE:
                    problems.append(f'{ENGINES[engine]} {name}: page {i + 1} looks different from MuPDF, '
                                    f'{difference:.1%} of its inked blocks differ')
            missing = characters(page.get_text()) - characters(result['text'][i])
            if missing:
                problems.append(f'{ENGINES[engine]} {name}: page {i + 1} lacks {missing.total()} characters '
                                f'MuPDF extracts: ' + ''.join(sorted(missing.elements()))[:40])
        if pairs:
            os.makedirs(os.path.join(out, 'contact-sheets'), exist_ok=True)
            contact_sheet(os.path.join(out, 'contact-sheets', f'{name}.png'), name, engine, pairs)
    return problems, measures


def main():
    if len(sys.argv) != 5 or sys.argv[1] not in ENGINES:
        print(f'Usage: python3 {sys.argv[0]} {"|".join(ENGINES)} PDF_FOLDER ENCRYPTED_FOLDER OUTPUT_FOLDER')
        sys.exit(2)
    engine, pdf_dir, encrypted_dir, out = sys.argv[1:]
    out = os.path.abspath(out)
    os.makedirs(out, exist_ok=True)
    all_files = files(pdf_dir, encrypted_dir)

    render(engine, all_files, out)
    problems, measures = [], []
    with ProcessPoolExecutor() as executor:
        for file_problems, file_measures in executor.map(functools.partial(check, engine, out), all_files):
            problems += file_problems
            measures += file_measures
    print(f'Rendered {len(all_files)} PDFs with {ENGINES[engine]}, and compared their first {MAX_PAGES} '
          f'pages and the text of every page with MuPDF.')
    if measures:
        ratio = min(measures, key=lambda m: m[0])
        difference = max(measures, key=lambda m: m[1])
        print(f'The smallest ink ratio is {ratio[0]:.2f}, on {ratio[2]} (at least {MIN_INK_RATIO}); the largest '
              f'block difference is {difference[1]:.1%}, on {difference[2]} (at most {MAX_DIFFERENCE:.0%}).')

    for problem in problems:
        print(f'::error::{problem}')
    if problems:
        print(f'{len(problems)} problem{"" if len(problems) == 1 else "s"} found.')
        sys.exit(1)
    print('No problems found.')


if __name__ == '__main__':
    main()
