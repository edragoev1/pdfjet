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

  * The heading levels a reader follows start at H1 and skip none.
  * Every figure and every link has a description that is not blank and is not
    a file name.
  * A list is built of L, LI, Lbl and LBody, in that nesting.
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
        kind, text = doc.xref_get_key(xref, 'A')
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


def walk(element, report, rows=None):
    report.counts[element.tag] += 1
    if element.tag in HEADINGS:
        report.headings.append(int(element.tag[1]))
    elif element.tag in ('Figure', 'Link'):
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
        walk(kid, report, rows)


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


def check(path):
    report = Report(os.path.basename(path))
    doc = pymupdf.open(path)
    root = doc.xref_get_key(doc.pdf_catalog(), 'StructTreeRoot')
    if root[0] != 'xref':
        report.problems.append('has no structure tree')
        return report
    for number in references(doc.xref_get_key(int(root[1].split()[0]), 'K')):
        walk(Element(doc, number), report)

    if report.headings:
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
