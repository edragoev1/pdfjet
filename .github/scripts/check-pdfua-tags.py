"""Checks what the tags of the PDF/UA examples stand for.

veraPDF checks that a file is built as PDF/UA requires: a structure tree, a
title, a language, an Alt on every figure. It cannot check what the tags mean,
which is the part of the Matterhorn Protocol marked "human verification
required": whether the heading of a page is tagged as a heading, whether the
header row of a table is tagged as a header, whether the description of a
figure describes it. This reads the structure tree of each example and reports
what a reviewer would otherwise have to look for by hand.

    python3 .github/scripts/check-pdfua-tags.py build/check-examples/pdfs/java

The checks are:

  * The heading levels a reader follows start at H1 and skip none, and a
    document with headings has bookmarks, as PAC asks.
  * Every figure has a description that is not blank and is not a file name,
    and so does every link that holds no text of its own, like a point of a
    chart; a link that holds its text is described by it.
  * Every Link holds its annotation, and, as PAC asks, the text it is on: a
    Link that holds only its annotation is noted.
  * Every figure has a bounding box, a BBox of the Layout attributes, with an
    area: PDF/UA asks for one, and veraPDF does not check it (PAC does).
  * Text contrasts with the background under it, 4.5:1, or 3:1 for text of
    18 points or 14 points bold, as WCAG 1.4.3 asks and PAC checks. The
    background is the color most of the box of each run of text is
    rendered in, on the first MAX_CONTRAST_PAGES pages.
  * The parent tree agrees with the structure tree: the element it gives for
    each marked content of a page has that marked content among its kids, and
    the element it gives for an annotation has the annotation. PAC reports
    an entry that does not as "Inconsistent entry found".
  * A list is built of L, LI, Lbl and LBody, in that nesting.
  * A Figure, a Link or an Annot, of the types PDF makes inline, that is a
    kid of the Document or another element that groups stands as a block
    and says so with Placement Block; and an element of text, a P, a heading
    or a Span, has no Alt of its own, which PAC warns of.
  * The cells of a table tile it: laid out as a browser lays out an HTML
    table, with ColSpan and RowSpan, they fill every square of the grid and
    none of them twice. Every header cell says what it heads with a Scope.

A document with no heading, and a table with no header row, are printed as
notes rather than failures: not every page has a heading, and not every table
has headers, which is what a reviewer decides.
"""
import os
import re
import sys
from collections import Counter, defaultdict

import pymupdf

HEADINGS = {'H1', 'H2', 'H3', 'H4', 'H5', 'H6'}
FILE_NAME = re.compile(r'[\w-]+\.(png|jpg|jpeg|bmp|gif|svg|pdf)\Z', re.I)


def references(value):
    """Returns the object numbers of a raw value, which is one reference or an
    array of them."""
    kind, text = value
    if kind == 'null':
        return []
    return [int(n) for n in re.findall(r'(\d+) \d+ R', text)]


def text_value(doc, xref, key):
    """Returns the string value of the key, or None when there is none. A text
    string is written in parentheses, or in hexadecimal between < and >."""
    kind, text = doc.xref_get_key(xref, key)
    if kind == 'string':
        return text
    if kind == 'xref':
        return text_value(doc, int(text.split()[0]), '')
    return None


class Element:
    """A structure element of the tree."""

    def __init__(self, doc, xref):
        self.doc = doc
        self.xref = xref
        kind, text = doc.xref_get_key(xref, 'S')
        self.tag = text.lstrip('/') if kind == 'name' else None
        self.alt = text_value(doc, xref, 'Alt')
        self.actual = text_value(doc, xref, 'ActualText')
        self.attributes = {}
        self.bbox = None
        kind, text = doc.xref_get_key(xref, 'A')
        box = re.search(r'/BBox\s*\[([^\]]*)\]', text) if kind == 'dict' else None
        if box:
            self.bbox = [float(v) for v in box.group(1).split()]
        if kind == 'dict':
            for name, value in re.findall(r'/(\w+)\s*(/?\w+)', text):
                self.attributes[name] = value
        elif kind == 'xref':
            other = int(text.split()[0])
            for name in doc.xref_get_keys(other):
                self.attributes[name] = doc.xref_get_key(other, name)[1]

    @property
    def description(self):
        return self.alt if self.alt is not None else self.actual

    @property
    def colspan(self):
        return self.span('ColSpan')

    @property
    def rowspan(self):
        return self.span('RowSpan')

    def span(self, name):
        try:
            return max(1, int(self.attributes.get(name, 1)))
        except ValueError:
            return 1

    @property
    def content(self):
        """Whether it holds marked content of its own, or elements: a Link
        that holds its text is described by the text."""
        kind, text = self.doc.xref_get_key(self.xref, 'K')
        bare = re.sub(r'<<.*?>>|\d+ 0 R', ' ', text or '')
        return bool(re.search(r'\d', bare)) or '/MCR' in (text or '') or any(True for _ in self.kids())

    @property
    def annotation(self):
        """Whether it holds an annotation, an OBJR."""
        kind, text = self.doc.xref_get_key(self.xref, 'K')
        if '/OBJR' in (text or ''):
            return True
        return any('/OBJR' in self.doc.xref_object(number)
                   for number in references((kind, text)))

    def kids(self):
        for number in references(self.doc.xref_get_key(self.xref, 'K')):
            if self.doc.xref_get_key(number, 'S')[0] == 'name':
                yield Element(self.doc, number)


class Report:
    def __init__(self, name):
        self.name = name
        self.problems = []
        self.notes = []
        self.counts = Counter()
        self.headings = []
        self.tables = []
        self.lists = []
        self.described = []
        self.boxes = []
        self.links = []
        self.unplaced = Counter()
        self.alt_on_text = Counter()


# The structure types that PDF makes inline, and those that group others: an
# inline element that is a kid of one that groups stands as a block, and says
# so with Placement Block, or PAC warns of it as a possibly inappropriate use.
INLINE_LEVEL = {'Figure', 'Formula', 'Form', 'Note', 'Link', 'Annot'}
GROUPING = {'Document', 'Part', 'Art', 'Sect', 'Div', 'BlockQuote', 'Caption', 'TOC',
            'TOCI', 'Index', 'NonStruct', 'Private'}
# The elements of text, whose text is read, and which PAC warns of when they
# have an Alt of their own
TEXT_ELEMENTS = {'P', 'H', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'Span', 'Lbl', 'LBody'}


def walk(element, report, rows=None, parent='Document'):
    report.counts[element.tag] += 1
    if element.tag in INLINE_LEVEL and parent in GROUPING and \
            element.attributes.get('Placement') != '/Block':
        report.unplaced[element.tag] += 1
    if element.tag in TEXT_ELEMENTS and element.alt is not None:
        report.alt_on_text[element.tag] += 1
    if element.tag in HEADINGS:
        report.headings.append(int(element.tag[1]))
    elif element.tag == 'Figure':
        report.described.append((element.tag, element.description))
        report.boxes.append((element.description, element.bbox))
    elif element.tag == 'Link':
        report.links.append((element.content, element.annotation))
        # A Link that holds its text is described by it; one over content
        # that is not tagged, like a point of a chart, by its Alt
        if not element.content:
            report.described.append((element.tag, element.description))
    elif element.tag == 'Table':
        rows = []
        report.tables.append(rows)
    elif element.tag == 'TR' and rows is not None:
        rows.append([(kid.tag, kid.colspan, kid.rowspan, kid.attributes.get('Scope'))
                     for kid in element.kids()])
    elif element.tag == 'L':
        report.lists.append([kid.tag for kid in element.kids()])
    for kid in element.kids():
        walk(kid, report, rows, element.tag)


def table_shape(rows):
    """The problems with the shape of a table, laid out as a browser lays out
    the cells of an HTML table: each cell goes in the first square of its row
    that no cell above it spans over, and covers ColSpan by RowSpan squares.
    A row that a span covers whole holds no cell and so is written as no TR,
    which is why the grid row is found rather than counted off the TRs."""
    width = sum(colspan for _, colspan, _, _ in rows[0])
    if width == 0:
        return ['has a first row of no columns']
    covered = defaultdict(set)      # The columns each grid row already holds.
    overlap = False
    r = 0
    for row in rows:
        while len(covered[r]) >= width:
            r += 1                  # A row the spans above cover whole.
        c = 0
        for _, colspan, rowspan, _ in row:
            while c in covered[r]:
                c += 1
            for r2 in range(r, r + rowspan):
                for c2 in range(c, c + colspan):
                    if c2 in covered[r2]:
                        overlap = True
                    covered[r2].add(c2)
            c += colspan
        r += 1
    problems = []
    widths = {len(columns) for columns in covered.values()}
    if widths != {width}:
        problems.append(f'has rows of {sorted(widths)} columns')
    if overlap:
        problems.append('has cells whose spans cover the same square twice')
    return problems


MAX_CONTRAST_PAGES = 5
# Colors of text that may contrast less, by example, with the reason: WCAG
# asks no contrast of text that is decoration.
CONTRAST_EXCEPTIONS = {
    ('Example_07.pdf', '#d4d4d4'): 'the DRAFT watermark, which is decoration',
}


def luminance(rgb):
    def channel(v):
        v /= 255
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    return 0.2126 * channel(rgb[0]) + 0.7152 * channel(rgb[1]) + 0.0722 * channel(rgb[2])


def contrast(a, b):
    light, dark = sorted((luminance(a), luminance(b)), reverse=True)
    return (light + 0.05) / (dark + 0.05)


def contrast_problems(doc, name):
    """Returns each color of text that does not contrast enough with the
    background under it, once, with the first text drawn in it."""
    found = {}
    for page in list(doc)[:MAX_CONTRAST_PAGES]:
        pix = page.get_pixmap(dpi=72, colorspace=pymupdf.csRGB)
        for block in page.get_text('dict')['blocks']:
            for line in block.get('lines', []):
                for span in line['spans']:
                    if not span['text'].strip():
                        continue
                    c = span['color']
                    fg = ((c >> 16) & 255, (c >> 8) & 255, c & 255)
                    x0, y0, x1, y1 = (int(v) for v in span['bbox'])
                    counts = Counter(pix.pixel(x, y)
                                     for y in range(max(0, y0), min(pix.height, y1 + 1))
                                     for x in range(max(0, x0), min(pix.width, x1 + 1), 2))
                    common = [color for color, _ in counts.most_common(2)]
                    background = next((color for color in common if color != fg), (255, 255, 255))
                    bold = 'Bold' in span['font'] or span['flags'] & 16
                    large = span['size'] >= 18 or (bold and span['size'] >= 14)
                    ratio = contrast(fg, background)
                    if ratio < (3.0 if large else 4.5) and (name, '#%02x%02x%02x' % fg) not in CONTRAST_EXCEPTIONS:
                        key = ('#%02x%02x%02x' % fg, '#%02x%02x%02x' % background)
                        found.setdefault(key, (ratio, span['text'].strip()[:30], page.number + 1))
    return [f'page {page}: text {fg} on {bg} contrasts {ratio:.2f}:1, less than WCAG asks, as in {text!r}'
            for (fg, bg), (ratio, text, page) in found.items()]


def number_tree(doc, xref):
    """Returns the entries of the number tree whose root is the object xref, as
    {key: raw value}, from its Nums and the Nums of its Kids."""
    entries = {}
    kind, text = doc.xref_get_key(xref, 'Nums')
    if kind == 'array':
        for key, value in re.findall(r'(\d+)\s*(\d+ \d+ R|\[[^\]]*\])', text):
            entries[int(key)] = value
    for kid in references(doc.xref_get_key(xref, 'Kids')):
        entries.update(number_tree(doc, kid))
    return entries


def parent_tree_problems(doc, tree):
    """Returns the entries of the parent tree whose element does not have the
    marked content or the annotation they are the parent of."""
    kind, text = doc.xref_get_key(tree, 'ParentTree')
    if kind != 'xref':
        return ['has no parent tree']
    entries = number_tree(doc, int(text.split()[0]))
    problems = []
    for page in doc:
        kind, text = doc.xref_get_key(page.xref, 'StructParents')
        if kind == 'int':
            value = entries.get(int(text))
            if value is None:
                problems.append(f'page {page.number + 1} has no entry in the parent tree')
                continue
            if value.endswith('R'):  # An array of its own
                value = doc.xref_object(int(value.split()[0]), compressed=True)
            for mcid, element in enumerate(int(n) for n in re.findall(r'(\d+) \d+ R', value)):
                kids = doc.xref_get_key(element, 'K')[1]
                marked = [int(n) for n in re.findall(r'(?<![/\d])(\d+)(?! \d+ R)(?![\d.])', re.sub(r'\d+ \d+ R', '', kids))]
                marked += [int(doc.xref_get_key(mcr, 'MCID')[1]) for mcr in references(('array', kids))
                           if doc.xref_get_key(mcr, 'MCID')[0] == 'int']
                marked += [int(n) for n in re.findall(r'/MCID\s+(\d+)', kids)]
                if mcid not in marked:
                    problems.append(f'page {page.number + 1}: the parent tree gives marked content {mcid} '
                                    f'to an element that does not have it')
        for annot in page.annots() or []:
            kind, text = doc.xref_get_key(annot.xref, 'StructParent')
            if kind == 'int':
                value = entries.get(int(text), '')
                kids = doc.xref_get_key(int(value.split()[0]), 'K')[1] if value.endswith('R') else ''
                if f'/Obj {annot.xref} 0 R' not in kids.replace('  ', ' '):
                    problems.append(f'page {page.number + 1}: the parent tree gives an annotation '
                                    f'to an element that does not have it')
    return problems


def check(path):
    report = Report(os.path.basename(path))
    doc = pymupdf.open(path)
    root = doc.xref_get_key(doc.pdf_catalog(), 'StructTreeRoot')
    if root[0] != 'xref':
        report.problems.append('has no structure tree')
        return report
    for number in references(doc.xref_get_key(int(root[1].split()[0]), 'K')):
        walk(Element(doc, number), report)
    report.problems += parent_tree_problems(doc, int(root[1].split()[0]))
    report.problems += contrast_problems(doc, report.name)

    if report.headings:
        # PAC asks a document with headings for bookmarks, which PDFjet makes
        # of its headings when it has none of its own
        if not doc.get_toc():
            report.problems.append('has headings and no bookmarks')
        if report.headings[0] != 1:
            report.problems.append(f'the first heading is H{report.headings[0]}, not H1')
        for before, after in zip(report.headings, report.headings[1:]):
            if after > before + 1:
                report.problems.append(
                    f'the heading levels skip from H{before} to H{after}')
                break
    else:
        report.notes.append('has no heading')

    for tag, description in report.described:
        what = 'a figure' if tag == 'Figure' else 'a link'
        if description is None:
            report.problems.append(f'{what} has no Alt and no ActualText')
        elif not description.strip():
            report.problems.append(f'{what} has a blank Alt')
        elif FILE_NAME.match(description.strip()):
            report.problems.append(f'{what} is described by the file name '
                                   f'{description.strip()!r}')

    # PDF/UA puts the annotation of a link in a Link element, and PAC warns
    # of a Link that holds only the annotation, and not the text it is on
    if any(not annotation for _, annotation in report.links):
        report.problems.append('a Link holds no annotation')
    for tag, n in sorted(report.unplaced.items()):
        report.problems.append(f'{n} {tag} elements stand as blocks without Placement Block')
    for tag, n in sorted(report.alt_on_text.items()):
        report.problems.append(f'{n} {tag} elements of text have an Alt, which PAC warns of')
    alone = sum(1 for content, _ in report.links if not content)
    if alone:
        report.notes.append(f'{alone} of {len(report.links)} links hold no content, '
                            'like the points of a chart, whose figure stands for them')

    for description, box in report.boxes:
        name = (description or '').strip()[:40]
        if box is None:
            report.problems.append(f'the figure {name!r} has no BBox')
        elif len(box) != 4 or box[2] <= box[0] or box[3] <= box[1]:
            report.problems.append(f'the figure {name!r} has the BBox {box}, which has no area')

    for i, rows in enumerate(report.tables, start=1):
        if not rows:
            continue
        for problem in table_shape(rows):
            report.problems.append(f'table {i} {problem}')
        headers = [cell for row in rows for cell in row if cell[0] == 'TH']
        if not headers:
            report.notes.append(f'table {i} of {len(rows)} rows has no header row')
        for _, _, _, scope in headers:
            if scope is None:
                report.problems.append(f'table {i} has a header cell with no Scope')
                break

    for i, tags in enumerate(report.lists, start=1):
        other = sorted({tag for tag in tags if tag != 'LI'})
        if other:
            report.problems.append(f'list {i} holds {other}, and not only LI')

    return report


def main():
    directory = sys.argv[1]
    paths = sorted(os.path.join(directory, name)
                   for name in os.listdir(directory) if name.endswith('.pdf'))
    reports = []
    for path in paths:
        try:
            report = check(path)
        except Exception as e:                      # A file that cannot be read
            print(f'{os.path.basename(path)}: cannot be checked: {e}')
            continue
        if report.counts:
            reports.append(report)

    problems = 0
    for report in reports:
        for note in report.notes:
            print(f'{report.name}: {note}')
    for report in reports:
        for problem in report.problems:
            print(f'::error::{report.name}: {problem}')
            problems += 1

    counts = Counter()
    for report in reports:
        counts.update(report.counts)
    print(f'Read the structure tree of {len(reports)} tagged documents: '
          + ', '.join(f'{tag} {n}' for tag, n in sorted(counts.items())))
    if problems:
        print(f'{problems} problem{"" if problems == 1 else "s"} found.')
        sys.exit(1)
    print('No problems found.')


if __name__ == '__main__':
    main()
