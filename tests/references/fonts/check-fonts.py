"""Checks the fonts PDFjet reads against fontTools and Adobe's AFM files.

    tests/references/fonts/fetch-fonts.sh .reference-fonts
    python3 tests/references/fonts/check-fonts.py .reference-fonts [--port go] [--jobs N] [--only SUBSTRING]

It needs fontTools; the version it was written with is FONTTOOLS below, and a
run says the version it had.

The fonts are every .otf and .ttf under fonts/ and the .stream file made from
each, the six fonts fetch-fonts.sh fetches, and the 14 core fonts. The port
loads each one as a user does, and says what it read: see TestFontReference in
src/fontreference_test.go. A font file is compared with what fontTools reads
of it, and a .stream file with what fontTools reads of the font it was made
from. The check fails on a font when the port:

- cannot load it;
- reads a different PostScript name (name ID 6, Windows before Macintosh),
  units per em or bounding box (head), ascent, descent or line gap (hhea,
  which PDFjet uses rather than the typographic or Windows metrics of OS/2),
  cap height, underline position or thickness (post), first or last character
  (OS/2), or kind of outlines (a CFF table or not). The cap height is
  sCapHeight of OS/2 when the table is version 2 or later; a version 0 or 1
  table does not have it, and then it is the top, yMax, of the glyph of H in
  the glyf table when the font has TrueType outlines and an H, and else the
  ascent of hhea;
- reads a different number of advance widths (numberOfHMetrics), or a
  different width for any of them (hmtx);
- maps any character to a different glyph than the Windows Unicode BMP
  subtable, (3, 1), maps it, among the characters from the first to the last
  that OS/2 gives, which are the ones PDFjet looks up;
- leaves out a character of the BMP that fontTools' best character map has,
  because it is outside that range or only in another subtable;
- gives StringWidth, for any character of the BMP, another width than the
  font's: that of its glyph; that of .notdef, which PDFjet draws, for a
  character the font does not have, in the range or outside it; that of the
  space for a control character, U+0000 to U+001F and U+007F to U+009F; and
  none for the ZWNJ, ZWJ, LRM, RLM and BOM;
- reads a mark of a MarkToBase or MarkToLigature subtable of GPOS with a
  different class or anchor, a base or ligature with different anchors (the
  anchors of the first letter of a ligature), or a mark on a mark with a
  different offset (the first lookup that has the pair) than GPOS has.

A core font fails when the port reads a different name, bounding box or
underline from its AFM file; an ascent or descent other than the top and the
bottom of that bounding box, which PDFjet uses in place of the AFM's Ascender
and Descender; draws a character with another code than WinAnsiEncoding
gives it (Symbol and ZapfDingbats: the code of the character itself), but
for U+007F, a control that PDFjet draws as a space where WinAnsiEncoding has
a bullet; gives it another width than the AFM gives the glyph
WinAnsiEncoding has at that code; or kerns a pair of characters by another amount than the AFM's KPX
lines, or kerns a pair they do not have. A KPX pair of a glyph WinAnsi does
not have, like Lcaron, cannot be drawn and is counted, not failed, and so is
a code of Symbol or ZapfDingbats that has no glyph, as the AFM gives it no
width.

Some differences are counted and not failed, because they are how PDFjet is
made: the characters past the BMP, which a PDFjet font does not have; the
kerning of an OpenType font, which PDFjet does not do (SetKernPairs kerns the
core fonts only); and the typographic metrics a font asks for with
USE_TYPO_METRICS, as PDFjet uses hhea. The differences that are understood
are in known-differences.txt, one per line, as "font field: reason", and are
counted rather than failed too. A difference listed there that is gone fails
the check, so that the list does not keep what was fixed.
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

import fontTools
from fontTools import afmLib
from fontTools.ttLib import TTFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
FONTTOOLS = '4.65.0'
# How far apart two widths from StringWidth may be, in font units.
TOLERANCE = 0.01
# The characters StringWidth gives no width: they are not drawn.
NOT_DRAWN = {0x200C, 0x200D, 0x200E, 0x200F, 0xFEFF}
# The control characters, which PDFjet draws as a space.
CONTROLS = set(range(0x20)) | set(range(0x7F, 0xA0))
CORE_FONTS = ['Courier', 'Courier-Bold', 'Courier-BoldOblique', 'Courier-Oblique',
              'Helvetica', 'Helvetica-Bold', 'Helvetica-BoldOblique', 'Helvetica-Oblique',
              'Symbol', 'Times-Bold', 'Times-BoldItalic', 'Times-Italic', 'Times-Roman',
              'ZapfDingbats']
FETCHED = ['roboto/Roboto[wdth,wght].ttf', 'inter/Inter-Regular.ttf', 'dejavu/DejaVuSans.ttf',
           'liberation/LiberationSerif-Regular.ttf', 'sourcehan/SourceHanSansJP-Regular.otf',
           'vera/Vera.ttf']
# The names PDFBox gives in WinAnsiEncoding.java for the glyphs that ISO
# 32000, Annex D, puts at 240 and 255 octal: the space and the hyphen, which
# are the names the AFM files have.
AFM_NAME = {'nbspace': 'space', 'sfthyphen': 'hyphen'}


def run_port(port, fonts, out):
    """Runs the port's harness on the fonts and returns what it read, by path."""
    if port != 'go':
        sys.exit(f'The {port} port has no harness yet.')
    listing = os.path.join(out, 'fonts.txt')
    result = os.path.join(out, 'fonts.jsonl')
    with open(listing, 'w', encoding='utf-8') as f:
        f.write('\n'.join(fonts) + '\n')
    env = dict(os.environ, PDFJET_FONT_REFERENCE=listing, PDFJET_FONT_REFERENCE_OUT=result)
    subprocess.run(['go', 'test', './src', '-run', '^TestFontReference$', '-count=1'],
                   cwd=ROOT, env=env, check=True, stdout=subprocess.DEVNULL)
    offsets = {}
    with open(result, 'rb') as f:
        offset = 0
        for line in f:
            path = json.loads(re.match(rb'\{"path":("(?:[^"\\]|\\.)*")', line).group(1))
            offsets[path] = offset
            offset += len(line)
    return result, offsets


def read_port(result, offset):
    with open(result, 'rb') as f:
        f.seek(offset)
        return json.loads(f.readline())


# The reference: what fontTools reads of a font.

def ps_name(font):
    windows = mac = None
    for record in font['name'].names:
        if record.nameID == 6:
            key = (record.platformID, record.platEncID, record.langID)
            if key == (3, 1, 0x409):
                windows = record.toUnicode()
            elif key == (1, 0, 0):
                mac = record.toUnicode()
    return windows if windows is not None else mac


def windows_bmp_cmap(font):
    """The first (3, 1) subtable, the one PDFjet reads, or None."""
    for table in font['cmap'].tables:
        if (table.platformID, table.platEncID) == (3, 1):
            return table
    return None


def gpos_marks(font):
    """The marks of GPOS as PDFjet keeps them; see getGposTable in src/otf.go."""
    marks, bases, mark_to_mark = [], [], {}
    if 'GPOS' not in font or font['GPOS'].table.LookupList is None:
        return marks, bases, mark_to_mark
    gid = font.getGlyphID
    for lookup in font['GPOS'].table.LookupList.Lookup:
        for sub in lookup.SubTable:
            kind = lookup.LookupType
            if kind == 9:
                kind, sub = sub.ExtensionLookupType, sub.ExtSubTable
            if kind in (4, 5) and sub.Format == 1:
                m = {}
                for glyph, record in zip(sub.MarkCoverage.glyphs, sub.MarkArray.MarkRecord):
                    m[str(gid(glyph))] = [record.Class, record.MarkAnchor.XCoordinate,
                                          record.MarkAnchor.YCoordinate]
                b = {}
                if kind == 4:
                    for glyph, record in zip(sub.BaseCoverage.glyphs, sub.BaseArray.BaseRecord):
                        b[str(gid(glyph))] = anchors_of(record.BaseAnchor)
                else:
                    for glyph, attach in zip(sub.LigatureCoverage.glyphs,
                                             sub.LigatureArray.LigatureAttach):
                        if attach.ComponentCount:
                            b[str(gid(glyph))] = anchors_of(attach.ComponentRecord[0].LigatureAnchor)
                marks.append(m)
                bases.append(b)
            elif kind == 6 and sub.Format == 1:
                mark2s = list(zip(sub.Mark2Coverage.glyphs, sub.Mark2Array.Mark2Record))
                for glyph1, record1 in zip(sub.Mark1Coverage.glyphs, sub.Mark1Array.MarkRecord):
                    a1 = record1.MarkAnchor
                    for glyph2, record2 in mark2s:
                        a2 = record2.Mark2Anchor[record1.Class]
                        if a2 is None:
                            continue
                        key = f'{gid(glyph2)} {gid(glyph1)}'
                        if key not in mark_to_mark:
                            mark_to_mark[key] = [a2.XCoordinate - a1.XCoordinate,
                                                 a2.YCoordinate - a1.YCoordinate]
    return marks, bases, mark_to_mark


def anchors_of(anchors):
    values = []
    for anchor in anchors:
        if anchor is None:
            values += [0, 0, 0]
        else:
            values += [1, anchor.XCoordinate, anchor.YCoordinate]
    return values


def has_kerning(font):
    if 'kern' in font:
        return True
    if 'GPOS' in font and font['GPOS'].table.FeatureList:
        return any(r.FeatureTag == 'kern' for r in font['GPOS'].table.FeatureList.FeatureRecord)
    return False


def cap_height(font):
    """sCapHeight of OS/2, else the top of the H of glyf, else the ascent."""
    os2 = font['OS/2']
    if os2.version >= 2:
        return os2.sCapHeight
    # The H PDFjet reads: that of the (3, 1) subtable, in the range of OS/2.
    subtable = windows_bmp_cmap(font)
    h = subtable.cmap.get(0x48) if subtable else None
    if 'glyf' in font and h is not None and os2.usFirstCharIndex <= 0x48 <= os2.usLastCharIndex:
        glyph = font['glyf'][h]
        if font.getGlyphID(h) != 0 and hasattr(glyph, 'yMax'):  # An empty glyph has no box.
            return glyph.yMax
    return font['hhea'].ascent


def reference_of(path):
    """What fontTools reads of a font, in the terms of the harness."""
    font = TTFont(path, lazy=True)
    head, hhea, os2, post = font['head'], font['hhea'], font['OS/2'], font['post']
    order = font.getGlyphOrder()
    hmtx = font['hmtx'].metrics
    widths = [hmtx[order[i]][0] for i in range(hhea.numberOfHMetrics)]
    first, last = os2.usFirstCharIndex, os2.usLastCharIndex
    ref = {
        'name': ps_name(font),
        'unitsPerEm': head.unitsPerEm,
        'bbox': [head.xMin, head.yMin, head.xMax, head.yMax],
        'ascent': hhea.ascent,
        'descent': hhea.descent,
        'lineGap': hhea.lineGap,
        'capHeight': cap_height(font),
        'underlinePosition': post.underlinePosition,
        'underlineThickness': post.underlineThickness,
        'firstChar': first,
        'lastChar': last,
        'cff': 'CFF ' in font,
        'widths': widths,
    }
    subtable = windows_bmp_cmap(font)
    ref['cmapFormat'] = subtable.format if subtable else None
    cmap = {}
    if subtable is not None:
        for c, glyph in subtable.cmap.items():
            g = font.getGlyphID(glyph)
            if first <= c <= last and c <= 0xFFFF and g != 0:
                cmap[c] = g
    ref['cmap'] = cmap
    best = font.getBestCmap() or {}
    ref['past BMP'] = sum(1 for c in best if c > 0xFFFF)
    ref['left out'] = sorted(c for c in best if c <= 0xFFFF and c not in cmap and font.getGlyphID(best[c]) != 0)
    ref['markAnchors'], ref['baseAnchors'], ref['markToMark'] = gpos_marks(font)
    ref['kerning'] = has_kerning(font)
    ref['useTypoMetrics'] = bool(os2.fsSelection & (1 << 7)) and (
        (os2.sTypoAscender, os2.sTypoDescender, os2.sTypoLineGap) !=
        (hhea.ascent, hhea.descent, hhea.lineGap))
    return ref


def expected_advances(ref):
    """The width StringWidth should give each character of the BMP."""
    widths, cmap = ref['widths'], ref['cmap']
    advances = {}
    for c in range(0x10000):
        if 0xD800 <= c <= 0xDFFF:
            continue
        if c in NOT_DRAWN:
            advances[c] = 0
            continue
        # A character the font does not have, one outside the range from the
        # first to the last among them, which the cmap leaves out, is drawn
        # with .notdef, glyph 0.
        gid = cmap.get(0x20, 0) if c in CONTROLS else cmap.get(c, 0)
        advances[c] = widths[min(gid, len(widths) - 1)]
    return advances


def chars(cs):
    return ', '.join(f'U+{c:04X}' for c in cs[:5]) + (f' and {len(cs) - 5} more' if len(cs) > 5 else '')


def compare_font(port, ref):
    """The problems of what the port read of a font, as "field: what"."""
    if port.get('error'):
        return [f'load: {port["error"]}']
    problems = []
    for key in ['name', 'unitsPerEm', 'bbox', 'ascent', 'descent', 'lineGap', 'underlinePosition',
                'underlineThickness', 'firstChar', 'lastChar', 'cff']:
        if port[key] != ref[key]:
            problems.append(f'{key}: {port[key]}, fontTools {ref[key]}')
    if port['capHeight'] != ref['capHeight']:
        problems.append(f'capHeight: {port["capHeight"]}, fontTools {ref["capHeight"]}')

    if len(port['widths']) != len(ref['widths']):
        problems.append(f'widths: {len(port["widths"])} advance widths, fontTools {len(ref["widths"])}')
    else:
        wrong = [i for i, (a, b) in enumerate(zip(port['widths'], ref['widths'])) if a != b]
        if wrong:
            i = wrong[0]
            problems.append(f'widths: {len(wrong)} differ, the first glyph {i}: '
                            f'{port["widths"][i]}, fontTools {ref["widths"][i]}')

    cmap = {c: g for c, g in port['cmap']}
    wrong = sorted(c for c in set(cmap) | set(ref['cmap']) if cmap.get(c, 0) != ref['cmap'].get(c, 0))
    if wrong:
        c = wrong[0]
        problems.append(f'cmap: {len(wrong)} characters map to other glyphs than in the (3, 1) '
                        f'subtable, the first U+{c:04X}: glyph {cmap.get(c, 0)}, fontTools '
                        f'{ref["cmap"].get(c, 0)}')
    if ref['left out']:
        problems.append(f'left out: {len(ref["left out"])} characters of the BMP that the font maps '
                        f'are not read: {chars(ref["left out"])}')

    advances = expected_advances(ref)
    wrong = []
    for first, last, width in port['advances']:
        for c in range(int(first), int(last) + 1):
            if abs(advances[c] - width) > TOLERANCE:
                wrong.append((c, width))
    if wrong:
        c, width = wrong[0]
        problems.append(f'StringWidth: {len(wrong)} characters, the first U+{c:04X}: {width:g}, '
                        f'the font {advances[c]}')

    for key in ['markAnchors', 'baseAnchors']:
        if len(port[key]) != len(ref[key]):
            problems.append(f'{key}: {len(port[key])} subtables, fontTools {len(ref[key])}')
            continue
        for i, (a, b) in enumerate(zip(port[key], ref[key])):
            wrong = sorted(set(a) | set(b), key=int)
            wrong = [g for g in wrong if a.get(g) != b.get(g)]
            if wrong:
                g = wrong[0]
                problems.append(f'{key}: {len(wrong)} glyphs of subtable {i} differ, the first '
                                f'glyph {g}: {a.get(g)}, fontTools {b.get(g)}')
                break
    a, b = port['markToMark'], ref['markToMark']
    wrong = [k for k in set(a) | set(b) if a.get(k) != b.get(k)]
    if wrong:
        k = sorted(wrong)[0]
        problems.append(f'markToMark: {len(wrong)} pairs of marks differ, the first "{k}": '
                        f'{a.get(k)}, fontTools {b.get(k)}')
    return problems


def check_font(args):
    """Compares a font file and its .stream file with what fontTools reads."""
    source, keys, result, offsets = args
    try:
        ref = reference_of(source)
    except Exception as e:
        return [(key, [f'fontTools: {type(e).__name__}: {e}'], {}) for key in keys], {}
    info = {'past BMP': ref['past BMP'], 'kerning': ref['kerning'],
            'useTypoMetrics': ref['useTypoMetrics']}
    results = []
    for key, path in keys.items():
        port = read_port(result, offsets[path])
        # What the font has is counted once, with the font file.
        results.append((key, compare_font(port, ref), {} if key.endswith('.stream') else info))
    return results


# The core fonts against the AFM files.

def win_ansi_names(afm_dir):
    """The glyph name of each code of WinAnsiEncoding, from PDFBox's table."""
    with open(os.path.join(afm_dir, 'WinAnsiEncoding.java'), encoding='utf-8') as f:
        text = f.read()
    names = {int(code, 8): AFM_NAME.get(name, name)
             for code, name in re.findall(r'\{(0[0-7]+), "(\w+)"\}', text)}
    # "In WinAnsiEncoding, all unused codes greater than 40 map to the bullet
    # character", ISO 32000, Annex D.
    for code in range(0o41, 256):
        names.setdefault(code, 'bullet')
    return names


def check_core_font(args):
    name, afm_dir, result, offset, win_ansi = args
    afm = afmLib.AFM(os.path.join(afm_dir, name + '.afm'))
    port = read_port(result, offset)
    key = f'core/{name}'
    if port.get('error'):
        return [(key, [f'load: {port["error"]}'], {})]
    problems = []
    bbox = list(afm.FontBBox)
    expected = {'name': afm.FontName, 'bbox': bbox, 'unitsPerEm': 1000,
                'underlinePosition': afm.UnderlinePosition,
                'underlineThickness': afm.UnderlineThickness,
                'ascent': bbox[3], 'descent': bbox[1], 'firstChar': 32, 'lastChar': 255}
    for field, value in expected.items():
        if port[field] != value:
            problems.append(f'{field}: {port[field]}, the AFM {value}')

    symbolic = name in ('Symbol', 'ZapfDingbats')
    # The glyph at each code, and the character drawn with each code.
    if symbolic:
        # The codes of the built-in encoding that have no glyph draw
        # nothing: the AFM gives them no width to compare.
        glyph_at = {c: glyph for glyph, (c, _, _) in afm._chars.items() if 32 <= c <= 255}
        char_of = {c: c for c in range(32, 256)}
    else:
        glyph_at = {c: win_ansi[c] for c in range(32, 256)}
        char_of = {}
        for c in range(32, 256):
            try:
                char_of[c] = ord(bytes([c]).decode('cp1252'))
            except UnicodeDecodeError:
                pass  # 129, 141, 143, 144 and 157 have no character.
        # U+007F, a control, is drawn as a space: 127 draws a bullet, which
        # the widths of the core fonts do not have.
        del char_of[127]
    port_chars = {int(c): u for c, u in port['coreChars'].items()}
    if port_chars != char_of:
        wrong = sorted(c for c in set(port_chars) | set(char_of) if port_chars.get(c) != char_of.get(c))
        c = wrong[0]
        problems.append(f'codes: {len(wrong)} codes are drawn for other characters, the first {c}: '
                        f'{port_chars.get(c)}, WinAnsiEncoding {char_of.get(c)}')

    def width_of(code):
        glyph = glyph_at.get(code)
        return afm[glyph][1] if glyph in afm._chars else None

    wrong = []
    for c, u in char_of.items():
        if c in glyph_at and port['coreWidths'].get(str(c)) != width_of(c):
            wrong.append(c)
    if wrong:
        c = wrong[0]
        problems.append(f'widths: {len(wrong)} codes, the first {c} ({glyph_at.get(c)}): '
                        f'{port["coreWidths"].get(str(c))}, the AFM {width_of(c)}')
    code_of = {u: c for c, u in char_of.items()}
    wrong = [(u, w) for u, _, w in port['advances'] if int(u) in code_of and
             code_of[int(u)] in glyph_at and abs(w - width_of(code_of[int(u)])) > TOLERANCE]
    if wrong:
        u, w = wrong[0]
        problems.append(f'StringWidth: {len(wrong)} characters, the first U+{int(u):04X}: {w:g}, '
                        f'the AFM {width_of(code_of[int(u)])}')

    # The kerning of each pair of codes that the AFM gives.
    codes_of = {}
    for c, glyph in glyph_at.items():
        if c in char_of:
            codes_of.setdefault(glyph, []).append(c)
    pairs = {}
    unusable = 0
    for (left, right), value in afm._kerning.items():
        if left not in codes_of or right not in codes_of:
            unusable += 1
            continue
        for c1 in codes_of[left]:
            for c2 in codes_of[right]:
                pairs[(c1, c2)] = value
    table = {(c1, c2): k for c1, c2, k in port.get('corePairs', []) if c1 in char_of and c2 in char_of}
    kerned = {(code_of[u1], code_of[u2]): k for u1, u2, k in port.get('coreKerning', [])}
    for what, got in [('kerning table', table), ('kerning', kerned)]:
        wrong = sorted(p for p in set(got) | set(pairs) if got.get(p) != pairs.get(p))
        if wrong:
            c1, c2 = wrong[0]
            problems.append(f'{what}: {len(wrong)} pairs of codes differ, the first {c1} {c2} '
                            f'({glyph_at.get(c1)} {glyph_at.get(c2)}): {got.get((c1, c2))}, '
                            f'the AFM {pairs.get((c1, c2))}')
    info = {'unusable pairs': unusable, 'pairs': len(afm._kerning),
            'no glyph': sum(1 for c in char_of if c not in glyph_at)}
    return [(key, problems, info)]


# The driver.

def read_known(path):
    known = {}
    with open(path, encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith('#'):
                name, _, reason = line.partition(': ')
                known[name] = reason
    return known


def font_files(fetched):
    """(source, {key: path}) for each font file and its .stream file."""
    fonts = []
    shipped = os.path.join(ROOT, 'fonts')
    for dirpath, dirnames, filenames in os.walk(shipped):
        dirnames.sort()
        for name in sorted(filenames):
            if name.endswith(('.otf', '.ttf')):
                path = os.path.join(dirpath, name)
                key = os.path.relpath(path, ROOT)
                keys = {key: path}
                if os.path.exists(path + '.stream'):
                    keys[key + '.stream'] = path + '.stream'
                fonts.append((path, keys))
    for name in FETCHED:
        path = os.path.abspath(os.path.join(fetched, name))
        if not os.path.exists(path):
            sys.exit(f'{path} is missing: run tests/references/fonts/fetch-fonts.sh first.')
        fonts.append((path, {name: path}))
    return fonts


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('fetched')
    parser.add_argument('--port', default='go')
    parser.add_argument('--jobs', type=int, default=os.cpu_count())
    parser.add_argument('--only', default='')
    args = parser.parse_args()

    print(f'fontTools {fontTools.version}' +
          ('' if fontTools.version == FONTTOOLS else f', not the {FONTTOOLS} it was written with'))
    known = read_known(os.path.join(HERE, 'known-differences.txt'))
    fonts = [(source, {k: p for k, p in keys.items() if args.only in k})
             for source, keys in font_files(args.fetched)]
    fonts = [(source, keys) for source, keys in fonts if keys]
    cores = [name for name in CORE_FONTS if args.only in f'core/{name}']
    afm_dir = os.path.join(args.fetched, 'afm')
    win_ansi = win_ansi_names(afm_dir)

    with tempfile.TemporaryDirectory() as out:
        paths = [p for _, keys in fonts for p in keys.values()] + [f'core/{n}' for n in cores]
        result, offsets = run_port(args.port, paths, out)
        tasks = [(source, keys, result, offsets) for source, keys in fonts]
        counts = Counter()
        kinds = Counter()
        seen = set()
        notes = Counter()
        failed = []
        with ProcessPoolExecutor(args.jobs) as pool:
            results = list(pool.map(check_font, tasks))
            results += list(pool.map(check_core_font, [(n, afm_dir, result, offsets[f'core/{n}'], win_ansi)
                                                       for n in cores]))
        for font_results in results:
            for key, problems, info in font_results:
                if info.get('past BMP'):
                    notes['fonts with characters past the BMP'] += 1
                    notes['characters past the BMP'] += info['past BMP']
                if info.get('kerning'):
                    notes['fonts whose kerning PDFjet does not apply'] += 1
                if info.get('useTypoMetrics'):
                    notes['fonts that ask for typographic metrics other than hhea'] += 1
                if info.get('no glyph'):
                    notes['codes of Symbol and ZapfDingbats that have no glyph'] += info['no glyph']
                if info.get('unusable pairs'):
                    notes['core kerning pairs of glyphs WinAnsi does not have'] += info['unusable pairs']
                    notes['core kerning pairs'] += info['pairs']
                left = []
                for problem in problems:
                    field = problem.split(':')[0]
                    if f'{key} {field}' in known:
                        seen.add(f'{key} {field}')
                        counts['known'] += 1
                    else:
                        left.append(problem)
                counts['fonts'] += 1
                if left:
                    counts['fail'] += 1
                    kinds.update({p.split(':')[0] for p in left})
                    failed.append(f'{key}: {"; ".join(left)}')
                else:
                    counts['pass'] += 1
        # A known difference of a font that was checked and no longer differs.
        checked = {key for font_results in results for key, _, _ in font_results}
        for entry in sorted(known):
            key = entry.rsplit(' ', 1)[0]
            if key in checked and entry not in seen:
                failed.append(f'{entry}: is listed in known-differences.txt and no longer differs')
                counts['fail'] += 1
                kinds['stale known difference'] += 1

    for line in failed:
        print(line)
    print()
    print(f'{counts["fonts"]} fonts: {counts["pass"]} pass, {counts["fail"]} fail, '
          f'{counts["known"]} known differences.')
    for kind, count in kinds.most_common():
        print(f'  {count:5d}  {kind}')
    for note, count in sorted(notes.items()):
        print(f'  Counted, not failed: {count} {note}')
    sys.exit(1 if failed else 0)


if __name__ == '__main__':
    main()
